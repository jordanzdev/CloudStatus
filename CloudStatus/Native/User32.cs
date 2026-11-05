using System.Runtime.InteropServices;

namespace CloudStatus.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    public uint cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ICONINFO
{
    public int fIcon;
    public uint xHotspot;
    public uint yHotspot;
    public nint hbmMask;
    public nint hbmColor;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NONCLIENTMETRICSW
{
    public uint cbSize;
    public int iBorderWidth;
    public int iScrollWidth;
    public int iScrollHeight;
    public int iCaptionWidth;
    public int iCaptionHeight;
    public LOGFONTW lfCaptionFont;
    public int iSmCaptionWidth;
    public int iSmCaptionHeight;
    public LOGFONTW lfSmCaptionFont;
    public int iMenuWidth;
    public int iMenuHeight;
    public LOGFONTW lfMenuFont;
    public LOGFONTW lfStatusFont;
    public LOGFONTW lfMessageFont;
    public int iPaddedBorderWidth;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    public nint hwnd;
    public uint message;
    public nint wParam;
    public nint lParam;
    public uint time;
    public POINT pt;
    public uint lPrivate;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WNDCLASSEXW
{
    public uint cbSize;
    public uint style;
    public delegate* unmanaged<nint, uint, nint, nint, nint> lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public nint hInstance;
    public nint hIcon;
    public nint hCursor;
    public nint hbrBackground;
    public char* lpszMenuName;
    public char* lpszClassName;
    public nint hIconSm;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DEV_BROADCAST_DEVICEINTERFACE_W
{
    public uint dbcc_size;
    public uint dbcc_devicetype;
    public uint dbcc_reserved;
    public Guid dbcc_classguid;
    public fixed char dbcc_name[1]; // null-terminated, longer than declared
}

internal static unsafe partial class User32
{
    const string Lib = "user32.dll";

    // Window messages
    public const uint WM_NULL = 0x0000;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_ACTIVATE = 0x0006;
    public const uint WM_CLOSE = 0x0010;
    public const uint WM_QUIT = 0x0012;
    public const uint WM_ENDSESSION = 0x0016;
    public const uint WM_SETFONT = 0x0030;
    public const uint WM_CONTEXTMENU = 0x007B;
    public const uint WM_SETICON = 0x0080;
    public const uint WM_NCDESTROY = 0x0082;
    public const uint WM_COMMAND = 0x0111;
    public const uint WM_TIMER = 0x0113;
    public const uint WM_LBUTTONDBLCLK = 0x0203;
    public const uint WM_POWERBROADCAST = 0x0218;
    public const uint WM_DEVICECHANGE = 0x0219;
    public const uint WM_DPICHANGED = 0x02E0;
    public const uint WM_USER = 0x0400;
    public const uint WM_APP = 0x8000;
    public const int WA_INACTIVE = 0;

    // Window styles
    public const uint WS_OVERLAPPED = 0x00000000;
    public const uint WS_CHILD = 0x40000000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_CAPTION = 0x00C00000;
    public const uint WS_VSCROLL = 0x00200000;
    public const uint WS_SYSMENU = 0x00080000;
    public const uint WS_TABSTOP = 0x00010000;
    public const uint WS_EX_DLGMODALFRAME = 0x00000001;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_CLIENTEDGE = 0x00000200;
    public const uint WS_EX_CONTROLPARENT = 0x00010000;
    public const int CW_USEDEFAULT = unchecked((int)0x80000000);
    public const int COLOR_BTNFACE = 15;

    // Window placement
    public const int SW_SHOWNORMAL = 1;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint MONITOR_DEFAULTTONEAREST = 2;

    // Controls
    public const uint SS_LEFT = 0x0000;
    public const uint BS_PUSHBUTTON = 0x0000;
    public const uint BS_DEFPUSHBUTTON = 0x0001;
    public const uint BS_AUTOCHECKBOX = 0x0003;
    public const uint ES_AUTOHSCROLL = 0x0080;
    public const uint ES_NUMBER = 0x2000;
    public const uint CBS_DROPDOWNLIST = 0x0003;
    public const uint BM_GETCHECK = 0x00F0;
    public const uint BM_SETCHECK = 0x00F1;
    public const nint BST_UNCHECKED = 0;
    public const nint BST_CHECKED = 1;
    public const int BN_CLICKED = 0;
    public const uint CB_ADDSTRING = 0x0143;
    public const uint CB_GETCURSEL = 0x0147;
    public const uint CB_SETCURSEL = 0x014E;
    public const uint EM_SETSEL = 0x00B1;
    public const uint EM_SETLIMITTEXT = 0x00C5;

    // Dialog navigation and message boxes
    public const uint DM_GETDEFID = WM_USER + 0;
    public const int DC_HASDEFID = 0x534B;
    public const int IDOK = 1;
    public const int IDCANCEL = 2;
    public const int IDYES = 6;
    public const int IDNO = 7;
    public const uint MB_OK = 0x00000000;
    public const uint MB_YESNOCANCEL = 0x00000003;
    public const uint MB_YESNO = 0x00000004;
    public const uint MB_ICONQUESTION = 0x00000020;
    public const uint MB_ICONINFORMATION = 0x00000040;
    public const uint MB_ICONWARNING = 0x00000030;

    // Power broadcasts (wParam of WM_POWERBROADCAST)
    public const nint PBT_APMSUSPEND = 0x0004;
    public const nint PBT_APMRESUMESUSPEND = 0x0007;
    public const nint PBT_APMRESUMEAUTOMATIC = 0x0012;

    // Device notifications
    public const nint DBT_DEVICEARRIVAL = 0x8000;
    public const nint DBT_DEVICEREMOVECOMPLETE = 0x8004;
    public const uint DBT_DEVTYP_DEVICEINTERFACE = 5;
    public const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0;

    // Message loop
    public const uint INFINITE = 0xFFFFFFFF;
    public const uint WAIT_FAILED = 0xFFFFFFFF;
    public const uint QS_ALLINPUT = 0x04FF;
    public const uint MWMO_INPUTAVAILABLE = 0x0004;
    public const uint PM_REMOVE = 0x0001;

    // Menus
    public const uint MF_STRING = 0x0000;
    public const uint MF_SEPARATOR = 0x0800;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_NONOTIFY = 0x0080;
    public const uint TPM_RETURNCMD = 0x0100;

    // Icons and cursors
    public const int SM_CXICON = 11;
    public const int SM_CXSMICON = 49;
    public const nint ICON_SMALL = 0;
    public const nint ICON_BIG = 1;
    public const uint IMAGE_ICON = 1;
    public const uint LR_DEFAULTCOLOR = 0;
    public const nint IDC_ARROW = 32512;
    public const nint IDC_WAIT = 32514;

    // Text and system settings
    public const uint DT_CENTER = 0x0001;
    public const uint DT_VCENTER = 0x0004;
    public const uint DT_SINGLELINE = 0x0020;
    public const uint DT_NOPREFIX = 0x0800;
    public const uint SPI_GETNONCLIENTMETRICS = 0x0029;

    [LibraryImport(Lib)]
    public static partial ushort RegisterClassExW(WNDCLASSEXW* windowClass);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowExW(uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [LibraryImport(Lib)]
    public static partial nint DefWindowProcW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hwnd);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hwnd);

    [LibraryImport(Lib)]
    public static partial uint MsgWaitForMultipleObjectsEx(uint count, nint* handles, uint milliseconds, uint wakeMask, uint flags);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PeekMessageW(MSG* msg, nint hwnd, uint filterMin, uint filterMax, uint removeMessage);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TranslateMessage(MSG* msg);

    [LibraryImport(Lib)]
    public static partial nint DispatchMessageW(MSG* msg);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsDialogMessageW(nint dialog, MSG* msg);

    [LibraryImport(Lib)]
    public static partial void PostQuitMessage(int exitCode);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint RegisterWindowMessageW(string name);

    [LibraryImport(Lib)]
    public static partial nuint SetTimer(nint hwnd, nuint id, uint elapseMs, nint timerProc);

    [LibraryImport(Lib)]
    public static partial nuint SetCoalescableTimer(nint hwnd, nuint id, uint elapseMs, nint timerProc, uint toleranceMs);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool KillTimer(nint hwnd, nuint id);

    [LibraryImport(Lib)]
    public static partial nint CreatePopupMenu();

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenuW(nint menu, uint flags, nuint id, string? text);

    [LibraryImport(Lib)]
    public static partial int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint menu);

    [LibraryImport(Lib)]
    public static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport(Lib)]
    public static partial int GetSystemMetricsForDpi(int index, uint dpi);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(nint icon);

    [LibraryImport(Lib)]
    public static partial nint LoadImageW(nint instance, nint name, uint type, int width, int height, uint flags);

    [LibraryImport(Lib)]
    public static partial nint LoadCursorW(nint instance, nint name);

    [LibraryImport(Lib)]
    public static partial nint CreateIconIndirect(ICONINFO* info);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DrawTextW(nint hdc, string text, int count, RECT* rect, uint format);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfoForDpi(uint action, uint param, void* data, uint winIni, uint dpi);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hwnd, int command);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AdjustWindowRectExForDpi(RECT* rect, uint style, [MarshalAs(UnmanagedType.Bool)] bool menu, uint exStyle, uint dpi);

    [LibraryImport(Lib)]
    public static partial nint MonitorFromWindow(nint hwnd, uint flags);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(nint monitor, MONITORINFO* info);

    [LibraryImport(Lib)]
    public static partial nint SetFocus(nint hwnd);

    [LibraryImport(Lib)]
    public static partial nint GetFocus();

    [LibraryImport(Lib)]
    public static partial nint GetDlgItem(nint parent, int id);

    [LibraryImport(Lib)]
    public static partial uint GetDlgItemInt(nint parent, int id, int* translated, [MarshalAs(UnmanagedType.Bool)] bool signed);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetDlgItemInt(nint parent, int id, uint value, [MarshalAs(UnmanagedType.Bool)] bool signed);

    [LibraryImport(Lib)]
    public static partial nint SendMessageW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBoxW(nint owner, string text, string caption, uint type);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateWindow(nint hwnd);

    [LibraryImport(Lib)]
    public static partial nint SetCursor(nint cursor);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnableWindow(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowTextW(nint hwnd, string text);

    [LibraryImport(Lib)]
    public static partial nint RegisterDeviceNotificationW(nint recipient, void* filter, uint flags);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterDeviceNotification(nint notification);
}
