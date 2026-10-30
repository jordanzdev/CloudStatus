using System.Runtime.InteropServices;
using CloudStatus.Device;
using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>
/// Hidden top-level window that owns the timers and the tray icon. It is not a message-only window
/// because it must receive the TaskbarCreated broadcast.
/// </summary>
internal static class MainWindow
{
    const string ClassName = "CloudStatusHidden";

    static uint s_taskbarCreatedMessage;

    public static nint Handle { get; private set; }

    public static unsafe bool Create()
    {
        if (!WindowClass.Register(ClassName, &WindowProc))
            return false;

        Handle = CreateWindowExW(WS_EX_TOOLWINDOW, ClassName, "CloudStatus", WS_OVERLAPPED,
            0, 0, 0, 0, 0, 0, AppInstance.Handle, 0);
        s_taskbarCreatedMessage = RegisterWindowMessageW("TaskbarCreated");
        return Handle != 0;
    }

    /// <summary>Destroying the window ends the app (WM_DESTROY shuts down and quits the loop).</summary>
    public static void Close()
    {
        if (Handle != 0)
            DestroyWindow(Handle);
    }

    static bool TryHandle(uint message, nint wParam, nint lParam)
    {
        switch (message)
        {
            case WM_TIMER when (nuint)wParam == TrayIcon.AddRetryTimerId:
                TrayIcon.OnAddRetryTimer();
                return true;

            case WM_TIMER when (nuint)wParam == HeadsetLink.DeadlineTimerId:
                HeadsetLink.OnDeadlineTimer();
                return true;

            case TrayIcon.CallbackMessage:
                App.OnTrayEvent(TrayIcon.ParseCallback(wParam, lParam, out POINT anchor), anchor);
                return true;

            case WM_DEVICECHANGE:
                DongleWatcher.OnDeviceChange(wParam, lParam);
                return true;

            case WM_DPICHANGED:
                TrayIcon.OnDpiChanged((uint)wParam & 0xFFFF);
                return true;

            case WM_ENDSESSION:
                // Session end or an installer closing the app through the Restart Manager.
                if (wParam != 0)
                    Close();
                return true;

            case WM_CLOSE:
                Close();
                return true;

            case WM_DESTROY:
                App.Shutdown();
                PostQuitMessage(0);
                return true;

            default:
                if (message != s_taskbarCreatedMessage || message == 0)
                    return false;
                TrayIcon.OnTaskbarCreated();
                return true;
        }
    }

    [UnmanagedCallersOnly]
    static nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            return TryHandle(message, wParam, lParam) ? 0 : DefWindowProcW(window, message, wParam, lParam);
        }
        catch
        {
            return 0; // an exception must never cross into native code
        }
    }
}
