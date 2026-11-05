using System.Runtime.InteropServices;

namespace CloudStatus.Native;

/// <summary>Window frame attributes for the settings window (spec 013, REQ-013-07).</summary>
internal static unsafe partial class Dwmapi
{
    const string Lib = "dwmapi.dll";

    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    public const int DWMWCP_ROUND = 2;
    public const int DWMSBT_MAINWINDOW = 2; // Mica

    [LibraryImport(Lib)]
    public static partial int DwmSetWindowAttribute(nint hwnd, int attribute, void* value, uint size);
}
