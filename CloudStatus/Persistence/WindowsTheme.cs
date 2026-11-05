namespace CloudStatus.Persistence;

/// <summary>
/// Reads Windows' own light and dark choices (spec 013, REQ-013-01). Called at start-up and on a
/// theme change only. These are Windows' values: the app's settings are still read only at
/// start-up (spec 007).
/// </summary>
internal static class WindowsTheme
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string AppsValue = "AppsUseLightTheme";
    const string TaskbarValue = "SystemUsesLightTheme";

    /// <summary>Each value is null when missing or not a DWORD.</summary>
    public static void Read(out uint? appsUseLightTheme, out uint? systemUsesLightTheme)
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(KeyPath);
        appsUseLightTheme = key.TryGetDword(AppsValue, out uint apps) ? apps : null;
        systemUsesLightTheme = key.TryGetDword(TaskbarValue, out uint taskbar) ? taskbar : null;
    }
}
