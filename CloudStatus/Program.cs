using CloudStatus.Core;

namespace CloudStatus;

internal static class Program
{
    const string InstanceMutexName = @"Local\CloudStatus";

    [STAThread]
    static int Main(string[] args)
    {
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
