namespace CloudStatus.Core;

/// <summary>Version of the running app (major.minor.patch), set at build time with -p:Version.</summary>
internal static class AppVersion
{
    public static readonly Version Current = Normalize(typeof(AppVersion).Assembly.GetName().Version);

    public static string CurrentText => ToText(Current);

    public static string ToText(Version version) => $"{version.Major}.{version.Minor}.{version.Build}";

    /// <summary>Keeps major.minor.patch only, so "1.2.3" and "1.2.3.0" compare as equal.</summary>
    public static Version Normalize(Version? version) =>
        version is null ? new Version(0, 0, 0) : new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
}
