namespace CloudStatus.Persistence;

/// <summary>
/// What the MSI records under HKCU\Software\CloudStatus\Install (spec 009). Read once at start-up,
/// like the settings (spec 007).
/// </summary>
internal static class InstallRegistration
{
    const string KeyPath = @"Software\CloudStatus\Install";
    const string ExecutableValue = "Executable";

    /// <summary>True when this process is the copy installed by the MSI, not a portable copy.</summary>
    public static bool IsRunningInstalledCopy { get; private set; }

    public static void Initialize()
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(KeyPath);
        string? installed = key.GetString(ExecutableValue);
        IsRunningInstalledCopy = installed != null &&
            string.Equals(installed, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
    }
}
