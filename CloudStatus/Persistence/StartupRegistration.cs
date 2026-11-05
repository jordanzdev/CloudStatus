using CloudStatus.Core;

namespace CloudStatus.Persistence;

/// <summary>
/// "Start with Windows" through HKCU\Software\Microsoft\Windows\CurrentVersion\Run (spec 007). Its
/// state is the presence of the value, read once at start-up and kept up to date on save.
/// </summary>
internal static class StartupRegistration
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "CloudStatus";

    static string? ExecutablePath => Environment.ProcessPath is { Length: > 0 } path ? path : null;

    public static bool IsEnabled { get; private set; }

    /// <summary>
    /// Reads the state. If startup is enabled but its command is not this exe's (the exe was moved,
    /// or the argument is missing), rewrites it.
    /// </summary>
    public static void Initialize()
    {
        string? registered;
        using (RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(RunKeyPath))
            registered = key.GetString(ValueName);

        IsEnabled = registered != null;
        string? path = ExecutablePath;
        if (registered == null || path == null || StartupCommand.IsCurrent(registered, path))
            return;

        using RegistryKeyHandle writable = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        writable.SetString(ValueName, StartupCommand.Build(path));
    }

    /// <summary>
    /// Removes the startup entry if it launches this exe (used by --uninstall, spec 009). An entry of
    /// another copy, such as a portable one, is left alone.
    /// </summary>
    public static bool UnregisterCurrentExecutable()
    {
        string? registered;
        using (RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(RunKeyPath))
            registered = key.GetString(ValueName);

        string? path = ExecutablePath;
        if (registered == null || path == null || !StartupCommand.Launches(registered, path))
            return true;

        using RegistryKeyHandle writable = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        return writable.DeleteValue(ValueName);
    }

    /// <summary>Enables or disables startup. Does nothing when it is already in the requested state.</summary>
    public static bool TrySet(bool enabled)
    {
        if (IsEnabled == enabled)
            return true;

        string? path = ExecutablePath;
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(RunKeyPath);
        bool changed = enabled
            ? path != null && key.SetString(ValueName, StartupCommand.Build(path))
            : key.DeleteValue(ValueName);
        if (changed)
            IsEnabled = enabled;
        return changed;
    }
}
