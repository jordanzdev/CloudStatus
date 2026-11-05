namespace CloudStatus.Core;

/// <summary>The command written to the Run key for "Start with Windows" (spec 007).</summary>
internal static class StartupCommand
{
    /// <summary>Added to the startup entry, so a launch at sign-in stays silently in the tray.</summary>
    public const string Argument = "--startup";

    public static string Build(string executablePath) => Quote(executablePath) + " " + Argument;

    /// <summary>
    /// True if <paramref name="registered"/> is exactly the command for this exe. A moved exe or a
    /// missing argument is different; letter case is not, as in Windows paths.
    /// </summary>
    public static bool IsCurrent(string registered, string executablePath) =>
        string.Equals(registered, Build(executablePath), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True if <paramref name="registered"/> launches this exe, with any arguments and in any letter
    /// case (spec 009). Another exe whose path starts the same way does not count.
    /// </summary>
    public static bool Launches(string registered, string executablePath)
    {
        string quoted = Quote(executablePath);
        return registered.StartsWith(quoted, StringComparison.OrdinalIgnoreCase) &&
            (registered.Length == quoted.Length || registered[quoted.Length] == ' ');
    }

    static string Quote(string path) => "\"" + path + "\"";
}
