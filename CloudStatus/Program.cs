namespace CloudStatus;

internal static class Program
{
    const string InstanceMutexName = @"Local\CloudStatus";

    [STAThread]
    static int Main()
    {
        using SingleInstance instance = SingleInstance.TryAcquire(InstanceMutexName);
        if (!instance.IsAcquired)
            return 0; // another instance is already running: exit silently

        try
        {
            return App.Run();
        }
        finally
        {
            App.Shutdown();
        }
    }
}
