using CloudStatus.Native;
using CloudStatus.Ui;

namespace CloudStatus;

/// <summary>Application lifetime and the use cases that span several components.</summary>
internal static class App
{
    static bool s_isShutDown;

    public static int Run()
    {
        Strings.Initialize();

        if (!MainWindow.Create())
            return 1;

        TrayIcon.Create(MainWindow.Handle);
        return MessageLoop.Run() ? 0 : 1;
    }

    public static void Exit() => MainWindow.Close();

    public static void OnTrayEvent(TrayEvent trayEvent, POINT anchor)
    {
        if (trayEvent == TrayEvent.ContextMenuRequested && TrayMenu.Show(MainWindow.Handle, anchor) == TrayCommand.Exit)
            Exit();
    }

    /// <summary>Releases every resource. Idempotent.</summary>
    public static void Shutdown()
    {
        if (s_isShutDown)
            return;
        s_isShutDown = true;

        TrayIcon.Remove();
    }
}
