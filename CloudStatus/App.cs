using CloudStatus.Device;
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
        HeadsetLink.Initialize(MainWindow.Handle);
        HeadsetLink.Changed += TrayIcon.Refresh;
        DongleWatcher.Changed += OnDongleChanged;
        DongleWatcher.Start(MainWindow.Handle); // after the icon, so a slow search never delays it
        return MessageLoop.Run() ? 0 : 1;
    }

    public static void Exit() => MainWindow.Close();

    static void OnDongleChanged()
    {
        if (DongleWatcher.IsConnected)
            HeadsetLink.Connect(DongleWatcher.Selected);
        else
            HeadsetLink.Disconnect();
        TrayIcon.Refresh();
    }

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

        HeadsetLink.Disconnect();
        DongleWatcher.Stop();
        TrayIcon.Remove();
    }
}
