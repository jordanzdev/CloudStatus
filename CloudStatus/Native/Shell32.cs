using System.Runtime.InteropServices;

namespace CloudStatus.Native;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NOTIFYICONDATAW
{
    public uint cbSize;
    public nint hWnd;
    public uint uID;
    public uint uFlags;
    public uint uCallbackMessage;
    public nint hIcon;
    public fixed char szTip[128];
    public uint dwState;
    public uint dwStateMask;
    public fixed char szInfo[256];
    public uint uVersion; // union with uTimeout
    public fixed char szInfoTitle[64];
    public uint dwInfoFlags;
    public Guid guidItem;
    public nint hBalloonIcon;
}

internal static unsafe partial class Shell32
{
    public const uint NIM_ADD = 0x0;
    public const uint NIM_MODIFY = 0x1;
    public const uint NIM_DELETE = 0x2;
    public const uint NIM_SETVERSION = 0x4;

    public const uint NIF_MESSAGE = 0x01;
    public const uint NIF_ICON = 0x02;
    public const uint NIF_TIP = 0x04;
    public const uint NIF_INFO = 0x10;
    public const uint NIF_SHOWTIP = 0x80; // NOTIFYICON_VERSION_4 hides the standard tooltip without it

    public const uint NIIF_WARNING = 0x2;
    public const uint NIIF_NOSOUND = 0x10;
    public const uint NIIF_RESPECT_QUIET_TIME = 0x80;

    public const uint NOTIFYICON_VERSION_4 = 4;

    public const int TooltipCapacity = 128;
    public const int InfoCapacity = 256;
    public const int InfoTitleCapacity = 64;

    [LibraryImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Shell_NotifyIconW(uint message, NOTIFYICONDATAW* data);
}
