using CloudStatus.Core;
using CloudStatus.Device;
using CloudStatus.Native;
using static CloudStatus.Native.Shell32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

internal enum TrayEvent
{
    None,
    ContextMenuRequested,
    Activated,
}

/// <summary>Notification-area icon: the headset status, drawn in memory, and a tooltip.</summary>
internal static unsafe class TrayIcon
{
    public const uint CallbackMessage = WM_APP + 1;
    public const nuint AddRetryTimerId = 2;

    const uint IconId = 1;
    const uint AddRetryIntervalMs = 2000;
    const int MaxAddRetries = 30;
    const int FallbackIconSize = 16;
    const nint ApplicationIconResourceId = 32512; // id the C# compiler gives the ApplicationIcon group

    static nint s_window;
    static nint s_icon;
    static bool s_isAdded;
    static int s_addRetries;
    static uint s_dpi;
    static IconDescription s_shownIcon;
    static ShownState s_shownState;
    static BatteryNotice s_pendingNotice;
    static byte s_pendingPercent;

    readonly record struct ShownState(HeadsetPower Power, byte BatteryPercent, ChargeState Charge);

    static ShownState CurrentState => new(HeadsetLink.Power, HeadsetLink.BatteryPercent, HeadsetLink.Charge);

    public static void Create(nint window)
    {
        s_window = window;
        s_dpi = GetDpiForWindow(window);
        s_shownState = CurrentState;
        Redraw();
        Add();
    }

    /// <summary>NOTIFYICON_VERSION_4: LOWORD(lParam) is the event and wParam holds the anchor point.</summary>
    public static TrayEvent ParseCallback(nint wParam, nint lParam, out POINT anchor)
    {
        anchor = new POINT
        {
            X = (short)((long)wParam & 0xFFFF),
            Y = (short)(((long)wParam >> 16) & 0xFFFF),
        };
        return ((uint)lParam & 0xFFFF) switch
        {
            WM_CONTEXTMENU => TrayEvent.ContextMenuRequested,
            WM_LBUTTONDBLCLK or NIN_KEYSELECT => TrayEvent.Activated,
            _ => TrayEvent.None,
        };
    }

    /// <summary>Explorer restarted: the previous icon no longer exists.</summary>
    public static void OnTaskbarCreated()
    {
        s_isAdded = false;
        s_addRetries = 0;
        s_dpi = GetDpiForWindow(s_window);
        nint previous = Redraw();
        Add();
        DestroyIfLoaded(ref previous);
    }

    public static void OnDpiChanged(uint dpi)
    {
        s_dpi = dpi;
        Show(Redraw());
    }

    /// <summary>The taskbar theme or high contrast changed: the icon's colors follow (spec 013).</summary>
    public static void OnThemeChanged() => Show(CurrentIcon() != s_shownIcon ? Redraw() : 0);

    /// <summary>A new language or low battery limit: the tooltip and maybe the icon change (spec 007).</summary>
    public static void OnSettingsChanged()
    {
        s_shownState = CurrentState;
        Show(CurrentIcon() != s_shownIcon ? Redraw() : 0);
    }

    /// <summary>Shows the current state. Allocation-free unless it changed.</summary>
    public static void Refresh()
    {
        ShownState state = CurrentState;
        if (state == s_shownState)
            return;
        s_shownState = state;

        Show(CurrentIcon() != s_shownIcon ? Redraw() : 0);
    }

    /// <summary>
    /// Shows a silent low-battery notice (spec 006), or keeps the latest one until the icon can be
    /// added. Windows decides when it appears: Do Not Disturb sends it to the notification center.
    /// The notice shows the icon's own plate at large size (spec 013), or the warning icon if it
    /// cannot be drawn.
    /// </summary>
    public static void ShowNotice(BatteryNotice notice, byte percent)
    {
        if (notice == BatteryNotice.None)
            return;
        if (!s_isAdded)
        {
            s_pendingNotice = notice;
            s_pendingPercent = percent;
            return;
        }
        s_pendingNotice = BatteryNotice.None;

        nint largeIcon = StatusIcon.Create(CurrentIcon(), GetSystemMetricsForDpi(SM_CXICON, s_dpi));
        NOTIFYICONDATAW data = CreateBaseData();
        data.uFlags = NIF_INFO;
        data.dwInfoFlags = (largeIcon != 0 ? NIIF_USER | NIIF_LARGE_ICON : NIIF_WARNING) | NIIF_NOSOUND | NIIF_RESPECT_QUIET_TIME;
        data.hBalloonIcon = largeIcon;
        CopyText(Strings.NoticeText(notice, percent), data.szInfo, InfoCapacity);
        CopyText(Strings.NoticeTitle(notice), data.szInfoTitle, InfoTitleCapacity);
        Shell_NotifyIconW(NIM_MODIFY, &data);
        DestroyIfLoaded(ref largeIcon); // the shell has its own copy once the call returns
    }

    /// <summary>The previous icon is destroyed only once the shell has the new one.</summary>
    static void Show(nint previousIcon)
    {
        if (s_isAdded)
        {
            NOTIFYICONDATAW data = CreateStateData();
            Shell_NotifyIconW(NIM_MODIFY, &data);
        }
        DestroyIfLoaded(ref previousIcon);
    }

    public static void OnAddRetryTimer()
    {
        if (s_isAdded || ++s_addRetries > MaxAddRetries)
        {
            KillTimer(s_window, AddRetryTimerId);
            return;
        }
        Add();
    }

    public static void Remove()
    {
        if (s_window != 0)
            KillTimer(s_window, AddRetryTimerId);
        if (s_isAdded)
        {
            NOTIFYICONDATAW data = CreateBaseData();
            Shell_NotifyIconW(NIM_DELETE, &data);
            s_isAdded = false;
        }
        DestroyIfLoaded(ref s_icon);
    }

    static void Add()
    {
        NOTIFYICONDATAW data = CreateStateData();
        s_isAdded = Shell_NotifyIconW(NIM_ADD, &data) || Shell_NotifyIconW(NIM_MODIFY, &data);
        if (!s_isAdded)
        {
            // Explorer is not ready yet (session start-up): retry for a while.
            SetTimer(s_window, AddRetryTimerId, AddRetryIntervalMs, 0);
            return;
        }

        KillTimer(s_window, AddRetryTimerId);
        s_addRetries = 0;
        data.uVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIconW(NIM_SETVERSION, &data);
        ShowNotice(s_pendingNotice, s_pendingPercent);
    }

    /// <summary>
    /// Draws the current state and returns the previous icon for the caller to destroy. If drawing
    /// fails the previous icon stays, and with none to keep the logo stands in.
    /// </summary>
    static nint Redraw()
    {
        int size = GetSystemMetricsForDpi(SM_CXSMICON, s_dpi);
        if (size <= 0)
            size = FallbackIconSize;

        IconDescription description = CurrentIcon();
        nint icon = StatusIcon.Create(description, size);
        if (icon == 0 && s_icon == 0)
            icon = LoadImageW(AppInstance.Handle, ApplicationIconResourceId, IMAGE_ICON, size, size, LR_DEFAULTCOLOR);
        if (icon == 0)
            return 0;

        nint previous = s_icon;
        s_icon = icon;
        s_shownIcon = description;
        return previous;
    }

    static IconDescription CurrentIcon() =>
        IconDescription.Describe(HeadsetLink.Power, HeadsetLink.BatteryPercent, HeadsetLink.Charge, Config.Current.LowBatteryPercent,
            SystemTheme.Current);

    static void DestroyIfLoaded(ref nint icon)
    {
        if (icon != 0)
            DestroyIcon(icon);
        icon = 0;
    }

    static NOTIFYICONDATAW CreateBaseData() => new()
    {
        cbSize = (uint)sizeof(NOTIFYICONDATAW),
        hWnd = s_window,
        uID = IconId,
    };

    static NOTIFYICONDATAW CreateStateData()
    {
        NOTIFYICONDATAW data = CreateBaseData();
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = s_icon;
        CopyText(CurrentTooltip(), data.szTip, TooltipCapacity);
        return data;
    }

    static string CurrentTooltip() => Strings.Tooltip(HeadsetLink.Power, HeadsetLink.BatteryPercent, HeadsetLink.Charge);

    /// <summary>The second line of the tooltip, shown at the top of the menu.</summary>
    public static string CurrentStatusLine() => Strings.StatusLine(HeadsetLink.Power, HeadsetLink.BatteryPercent, HeadsetLink.Charge);

    static void CopyText(string text, char* destination, int capacity)
    {
        int length = Math.Min(text.Length, capacity - 1);
        text.AsSpan(0, length).CopyTo(new Span<char>(destination, length));
        destination[length] = '\0';
    }
}
