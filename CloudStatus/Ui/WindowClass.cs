using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

internal static unsafe class WindowClass
{
    /// <summary>Registers a window class with the arrow cursor. Returns false on failure.</summary>
    public static bool Register(string name, delegate* unmanaged<nint, uint, nint, nint, nint> windowProc, nint background = 0)
    {
        fixed (char* className = name)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = windowProc,
                hInstance = AppInstance.Handle,
                hCursor = LoadCursorW(0, IDC_ARROW),
                hbrBackground = background,
                lpszClassName = className,
            };
            return RegisterClassExW(&windowClass) != 0;
        }
    }
}
