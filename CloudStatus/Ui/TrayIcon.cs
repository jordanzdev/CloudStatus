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
}

/// <summary>Notification-area icon: the app logo and a tooltip.</summary>
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
    static string s_shownTooltip = "";

    public static void Create(nint window)
    {
        s_window = window;
        LoadIcon(GetDpiForWindow(window));
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
        return ((uint)lParam & 0xFFFF) == WM_CONTEXTMENU ? TrayEvent.ContextMenuRequested : TrayEvent.None;
    }

    /// <summary>Explorer restarted: the previous icon no longer exists.</summary>
    public static void OnTaskbarCreated()
    {
        s_isAdded = false;
        s_addRetries = 0;
        LoadIcon(GetDpiForWindow(s_window));
        Add();
    }

    public static void OnDpiChanged(uint dpi)
    {
        LoadIcon(dpi);
        Modify();
    }

    /// <summary>Shows the current state, only if its text changed.</summary>
    public static void Refresh()
    {
        if (CurrentTooltip() != s_shownTooltip)
            Modify();
    }

    static void Modify()
    {
        if (!s_isAdded)
            return;
        NOTIFYICONDATAW data = CreateStateData();
        Shell_NotifyIconW(NIM_MODIFY, &data);
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
    }

    /// <summary>Loads the logo at the small-icon size for <paramref name="dpi"/>, replacing the previous one.</summary>
    static void LoadIcon(uint dpi)
    {
        int size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
        if (size <= 0)
            size = FallbackIconSize;

        DestroyIfLoaded(ref s_icon); // the shell keeps its own copy of the icon currently shown
        s_icon = LoadImageW(AppInstance.Handle, ApplicationIconResourceId, IMAGE_ICON, size, size, LR_DEFAULTCOLOR);
    }

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
        s_shownTooltip = CurrentTooltip();
        CopyText(s_shownTooltip, data.szTip, TooltipCapacity);
        return data;
    }

    static string CurrentTooltip() => HeadsetLink.Power switch
    {
        HeadsetPower.On => Strings.TooltipBattery(HeadsetLink.BatteryPercent, HeadsetLink.Charge),
        HeadsetPower.Off => Strings.TooltipHeadsetOff,
        HeadsetPower.Checking => Strings.TooltipDongleConnected,
        _ => Strings.TooltipNoDongle,
    };

    static void CopyText(string text, char* destination, int capacity)
    {
        int length = Math.Min(text.Length, capacity - 1);
        text.AsSpan(0, length).CopyTo(new Span<char>(destination, length));
        destination[length] = '\0';
    }
}
