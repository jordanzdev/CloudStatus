using System.Runtime.InteropServices;
using CloudStatus.Core;
using CloudStatus.Native;
using CloudStatus.Persistence;
using static CloudStatus.Native.Comctl32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui.Settings;

/// <summary>
/// Plain Win32 settings window (spec 007) with the Windows 11 look of spec 013. Created when opened
/// and destroyed when closed, so no controls, fonts, brushes or icons stay in memory while it is
/// closed.
/// </summary>
internal static unsafe class SettingsWindow
{
    const string ClassName = "CloudStatusSettings";
    const uint Style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_CLIPCHILDREN;
    const uint ExStyle = WS_EX_DLGMODALFRAME | WS_EX_CONTROLPARENT;
    const int InitialSize = 100;
    const float HeadingScale = 1.2f;

    static bool s_isClassRegistered;
    static bool s_areCommonControlsReady;
    static nint s_window;
    static nint s_bodyFont;
    static nint s_headingFont;
    static nint s_lastFocus;
    static string? s_infoText;
    static WindowIcons s_icons;

    public static void Show()
    {
        if (s_window != 0)
        {
            ShowWindow(s_window, SW_SHOWNORMAL);
            SetForegroundWindow(s_window);
            return;
        }
        Create();
    }

    /// <summary>Closes without asking (used on exit).</summary>
    public static void Close()
    {
        if (s_window != 0)
            DestroyWindow(s_window);
    }

    /// <summary>Windows switched between light, dark or high contrast: repaint without reopening (REQ-013-03).</summary>
    public static void OnThemeChanged()
    {
        if (s_window == 0)
            return;
        SettingsPainter.ApplyTheme();
        ApplyFrame(s_window);
        ApplyControlThemes(s_window);
        RedrawWindow(s_window, null, 0, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_FRAME);
    }

    static void Create()
    {
        EnsureCommonControls();
        s_isClassRegistered = s_isClassRegistered || WindowClass.Register(ClassName, &WindowProc);
        if (!s_isClassRegistered)
            return;

        // Windows picks the monitor; the window is then centered in its work area.
        nint window = CreateWindowExW(ExStyle, ClassName, Strings.SettingsTitle, Style,
            CW_USEDEFAULT, 0, InitialSize, InitialSize, 0, 0, AppInstance.Handle, 0);
        if (window == 0)
            return;

        s_window = window;
        s_lastFocus = 0;
        s_infoText = null;
        SettingsPainter.ErrorTextId = 0;
        SettingsPainter.IsInfoWarning = false;
        MessageLoop.DialogWindow = window;

        SettingsPainter.ApplyTheme();
        ApplyFrame(window);
        SettingsControls.Create(window);
        ApplyControlThemes(window);
        ApplyDpi(window, GetDpiForWindow(window));
        CenterOnMonitor(window);
        SettingsForm.Populate(window, Config.Current, StartupRegistration.IsEnabled);

        ShowWindow(window, SW_SHOWNORMAL);
        SetForegroundWindow(window);
        SettingsControls.Focus(window, SettingsControls.LowBatteryNoticeId);
    }

    static void EnsureCommonControls()
    {
        if (s_areCommonControlsReady)
            return;
        var controls = new INITCOMMONCONTROLSEX
        {
            dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
            dwICC = ICC_STANDARD_CLASSES,
        };
        s_areCommonControlsReady = InitCommonControlsEx(&controls);
    }

    /// <summary>Dark title bar, rounded corners and Mica in the title bar, where the build has them (REQ-013-07).</summary>
    static void ApplyFrame(nint window)
    {
        WindowEffects effects = WindowEffects.ForBuild(SystemTheme.Build);
        // A failed call is ignored: Windows 10 keeps its usual frame.
        if (effects.DarkModeAttribute != WindowEffects.NoAttribute)
        {
            int isDark = SystemTheme.Current.IsWindowDark ? 1 : 0;
            Dwmapi.DwmSetWindowAttribute(window, effects.DarkModeAttribute, &isDark, sizeof(int));
        }
        if (effects.RoundCorners)
        {
            int corners = Dwmapi.DWMWCP_ROUND;
            Dwmapi.DwmSetWindowAttribute(window, Dwmapi.DWMWA_WINDOW_CORNER_PREFERENCE, &corners, sizeof(int));
        }
        if (effects.MicaTitleBar)
        {
            int backdrop = Dwmapi.DWMSBT_MAINWINDOW;
            Dwmapi.DwmSetWindowAttribute(window, Dwmapi.DWMWA_SYSTEMBACKDROP_TYPE, &backdrop, sizeof(int));
        }
    }

    /// <summary>The language list draws its own field and arrow: give it Windows' dark style in dark mode.</summary>
    static void ApplyControlThemes(nint window)
    {
        string? style = SystemTheme.Current.IsWindowDark ? UxTheme.DarkControlsClass : null;
        UxTheme.SetWindowTheme(SettingsControls.Get(window, SettingsControls.LanguageId), style, null);
    }

    /// <summary>Icons, fonts and control positions for <paramref name="dpi"/>. Also runs on WM_DPICHANGED.</summary>
    static void ApplyDpi(nint window, uint dpi)
    {
        s_icons.Apply(window, dpi);
        nint previousBody = s_bodyFont, previousHeading = s_headingFont;
        s_bodyFont = SystemFont.CreateMessageFont(dpi);
        s_headingFont = CreateHeadingFont(dpi);
        Relayout(window, dpi);
        DeleteFont(previousBody);
        DeleteFont(previousHeading);
    }

    /// <summary>The message font, semibold and a little larger, for section headings.</summary>
    static nint CreateHeadingFont(uint dpi)
    {
        LOGFONTW font = SystemFont.GetMessageFont(dpi);
        font.lfHeight = (int)MathF.Round(font.lfHeight * HeadingScale);
        font.lfWeight = Gdi32.FW_SEMIBOLD;
        return Gdi32.CreateFontIndirectW(&font);
    }

    /// <summary>Places the controls and sizes the window to fit them, keeping its top left corner.</summary>
    static void Relayout(nint window, uint dpi)
    {
        SettingsControls.ApplyLayout(window, dpi, s_bodyFont, s_headingFont, s_infoText);
        RECT frame = WindowFrame(dpi);
        RECT current;
        if (GetWindowRect(window, &current))
            SetWindowPos(window, 0, current.Left, current.Top, frame.Width, frame.Height, SWP_NOZORDER | SWP_NOACTIVATE);
        InvalidateRect(window, null, false);
    }

    static RECT WindowFrame(uint dpi)
    {
        RECT frame = new()
        {
            Right = Dpi.Scale(SettingsControls.ClientWidth, dpi),
            Bottom = SettingsControls.ClientHeight,
        };
        AdjustWindowRectExForDpi(&frame, Style, false, ExStyle, dpi);
        return frame;
    }

    static void CenterOnMonitor(nint window)
    {
        var monitor = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (!GetMonitorInfoW(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), &monitor))
            return;

        RECT workArea = monitor.rcWork;
        RECT frame = WindowFrame(GetDpiForWindow(window));
        SetWindowPos(window, 0,
            workArea.Left + (workArea.Width - frame.Width) / 2,
            workArea.Top + (workArea.Height - frame.Height) / 2,
            frame.Width, frame.Height, SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>
    /// Shows a message in the info bar at the top of the window (REQ-013-13): a warning, or a
    /// plain result. The window grows to make room.
    /// </summary>
    public static void ShowMessage(string text, bool isWarning)
    {
        nint window = s_window;
        if (window == 0)
            return;

        s_infoText = text;
        SettingsPainter.IsInfoWarning = isWarning;
        SettingsPainter.UpdateInfoBrush();
        SetWindowTextW(SettingsControls.Get(window, SettingsControls.InfoTextId), text);
        ShowWindow(SettingsControls.Get(window, SettingsControls.InfoTextId), SW_SHOWNORMAL);
        ShowWindow(SettingsControls.Get(window, SettingsControls.CloseInfoId), SW_SHOWNORMAL);
        Relayout(window, GetDpiForWindow(window));
        RedrawWindow(window, null, 0, RDW_INVALIDATE | RDW_ALLCHILDREN);
    }

    static void HideMessage(nint window)
    {
        s_infoText = null;
        ShowWindow(SettingsControls.Get(window, SettingsControls.InfoTextId), SW_HIDE);
        ShowWindow(SettingsControls.Get(window, SettingsControls.CloseInfoId), SW_HIDE);
        Relayout(window, GetDpiForWindow(window));
        SettingsControls.Focus(window, SettingsControls.LowBatteryNoticeId);
    }

    /// <summary>Puts the error under the field in place of its range, and selects the field (REQ-013-12).</summary>
    static void ReportInvalid(nint window, int controlId, string text)
    {
        ClearInvalid(window);
        int secondaryId = SettingsControls.SecondaryIdFor(controlId);
        SettingsPainter.ErrorTextId = secondaryId;
        nint secondary = SettingsControls.Get(window, secondaryId);
        SetWindowTextW(secondary, text);
        InvalidateRect(secondary, null, true);

        nint control = SettingsControls.Get(window, controlId);
        SetFocus(control);
        SendMessageW(control, EM_SETSEL, 0, -1);
    }

    static void ClearInvalid(nint window)
    {
        int secondaryId = SettingsPainter.ErrorTextId;
        if (secondaryId == 0)
            return;
        SettingsPainter.ErrorTextId = 0;
        nint secondary = SettingsControls.Get(window, secondaryId);
        SetWindowTextW(secondary, SettingsControls.RangeTextFor(secondaryId));
        InvalidateRect(secondary, null, true);
    }

    static void Save(nint window)
    {
        switch (SettingsForm.TryRead(window, Config.Current, out Config next, out bool startWithWindows))
        {
            case SettingsFormError.InvalidLowBatteryPercent:
                ReportInvalid(window, SettingsControls.LowBatteryPercentId, Strings.ErrorInvalidLowBatteryPercent);
                return;
            case SettingsFormError.InvalidPollMinutes:
                ReportInvalid(window, SettingsControls.PollMinutesId, Strings.ErrorInvalidPollMinutes);
                return;
        }

        switch (App.ApplySettings(next, startWithWindows))
        {
            case ApplySettingsResult.NotSaved:
                ShowMessage(Strings.ErrorNotSaved, isWarning: true);
                return; // the window stays open with the user's input
            case ApplySettingsResult.StartupNotChanged:
                ShowMessage(Strings.ErrorStartupNotChanged, isWarning: true);
                return; // saved and applied; the window stays open so the message can be read
        }
        DestroyWindow(window);
    }

    static void ConfirmClose(nint window)
    {
        switch (CloseConfirmation.Ask(window, s_icons.Big))
        {
            case CloseChoice.MinimizeToTray:
                DestroyWindow(window); // the app keeps running in the tray
                break;
            case CloseChoice.ExitApplication:
                App.Exit();
                break;
        }
    }

    /// <summary>Shows progress on the button itself while the check blocks the UI thread (spec 010).</summary>
    static void CheckForUpdates(nint window)
    {
        nint button = SettingsControls.Get(window, SettingsControls.CheckForUpdatesId);
        EnableWindow(button, false);
        SetCursor(LoadCursorW(0, IDC_WAIT));
        try
        {
            UpdateCheck.Run(window,
                status =>
                {
                    SetWindowTextW(button, status);
                    UpdateWindow(button);
                },
                ShowMessage);
        }
        finally
        {
            // Harmless if the window is already gone (the update closes the app).
            SetWindowTextW(button, Strings.ButtonCheckForUpdates);
            EnableWindow(button, true);
            SetCursor(LoadCursorW(0, IDC_ARROW));
        }
    }

    static void OnCommand(nint window, int id, int notification)
    {
        if (SettingsControls.IsNumber(id))
        {
            if (notification is EN_SETFOCUS or EN_KILLFOCUS)
                InvalidateRect(window, null, false); // the field's bottom line follows the focus
            else if (notification == EN_CHANGE && SettingsPainter.ErrorTextId == SettingsControls.SecondaryIdFor(id))
                ClearInvalid(window);
            return;
        }
        if (notification != BN_CLICKED)
            return;

        switch (id)
        {
            case SettingsControls.SaveId:
                Save(window);
                break;
            case SettingsControls.CheckForUpdatesId:
                CheckForUpdates(window);
                break;
            case SettingsControls.CloseInfoId:
                HideMessage(window);
                break;
            case SettingsControls.CancelId:
                DestroyWindow(window); // Cancel and Esc close without asking
                break;
        }
    }

    /// <summary>This is not a real dialog, so focus is saved and restored by hand.</summary>
    static bool OnActivate(bool isActive)
    {
        if (!isActive)
        {
            s_lastFocus = GetFocus();
            return true;
        }

        if (s_lastFocus == 0)
            return false;
        SetFocus(s_lastFocus);
        return true;
    }

    static void OnDpiChanged(nint window, uint dpi, RECT* suggestedBounds)
    {
        SetWindowPos(window, 0, suggestedBounds->Left, suggestedBounds->Top,
            suggestedBounds->Width, suggestedBounds->Height, SWP_NOZORDER | SWP_NOACTIVATE);
        ApplyDpi(window, dpi);
    }

    /// <summary>The window and its controls are gone by WM_NCDESTROY, so the icons, fonts and brushes can be freed.</summary>
    static void OnFinalDestroy()
    {
        s_window = 0;
        s_lastFocus = 0;
        s_infoText = null;
        MessageLoop.DialogWindow = 0;
        s_icons.Release();
        DeleteFont(s_bodyFont);
        DeleteFont(s_headingFont);
        s_bodyFont = s_headingFont = 0;
        SettingsPainter.Release();
    }

    static void DeleteFont(nint font)
    {
        if (font != 0)
            Gdi32.DeleteObject(font);
    }

    [UnmanagedCallersOnly]
    static nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            switch (message)
            {
                case WM_ACTIVATE:
                    if (OnActivate(((int)wParam & 0xFFFF) != WA_INACTIVE))
                        return 0;
                    break;

                case WM_COMMAND:
                    OnCommand(window, (int)wParam & 0xFFFF, ((int)wParam >> 16) & 0xFFFF);
                    return 0;

                case WM_NOTIFY when ((NMHDR*)lParam)->code == NM_CUSTOMDRAW:
                    return SettingsPainter.OnCustomDraw((NMCUSTOMDRAW*)lParam);

                case WM_CTLCOLOREDIT:
                case WM_CTLCOLORLISTBOX:
                case WM_CTLCOLORSTATIC:
                case WM_CTLCOLORBTN:
                    return SettingsPainter.OnControlColor(message, wParam, lParam);

                case WM_ERASEBKGND:
                    return 1; // WM_PAINT covers the whole client area

                case WM_PAINT:
                    SettingsPainter.Paint(window);
                    return 0;

                case DM_GETDEFID:
                    // IsDialogMessageW asks for the default button when Enter is pressed.
                    return (DC_HASDEFID << 16) | SettingsControls.SaveId;

                case WM_DPICHANGED:
                    OnDpiChanged(window, (uint)wParam & 0xFFFF, (RECT*)lParam);
                    return 0;

                case WM_CLOSE:
                    ConfirmClose(window); // X button, Alt+F4 or system menu
                    return 0;

                case WM_NCDESTROY:
                    OnFinalDestroy();
                    break;
            }
            return DefWindowProcW(window, message, wParam, lParam);
        }
        catch
        {
            return 0; // an exception must never cross into native code
        }
    }
}
