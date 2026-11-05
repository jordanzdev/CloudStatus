using CloudStatus.Core;

namespace CloudStatus;

internal static class Program
{
    const string InstanceMutexName = @"Local\CloudStatus";
    // Run by the MSI (spec 009); each one does its job and exits without starting the app.
    const string UninstallArgument = "--uninstall";
    const string CreateDesktopShortcutArgument = "--create-desktop-shortcut";

    [STAThread]
    static int Main(string[] args)
    {
        if (args is [UninstallArgument])
            return App.RemoveUserData();
        if (args is [CreateDesktopShortcutArgument])
            return App.CreateDesktopShortcut();

        using SingleInstance instance = SingleInstance.TryAcquire(InstanceMutexName);
        if (!instance.IsAcquired)
            return 0; // another instance is already running: exit silently

        try
        {
            // Started at sign-in it stays in the tray; started by hand it shows Settings.
            bool startedWithWindows = args is [StartupCommand.Argument];
            return App.Run(showSettings: !startedWithWindows);
        }
        finally
        {
            App.Shutdown();
        }
    }
}
