namespace CloudStatus.Core;

/// <summary>
/// The Windows theme as CloudStatus uses it (spec 013): dark or light apps, dark or light taskbar,
/// and high contrast. Decided from the raw registry values, without reading anything.
/// </summary>
internal readonly record struct Theme(bool IsAppDark, bool IsTaskbarDark, bool IsHighContrast)
{
    /// <summary>What Windows shows with no values at all: light apps, dark taskbar.</summary>
    public static readonly Theme Default = Decide(null, null, false);

    /// <summary>
    /// <paramref name="appsUseLightTheme"/> and <paramref name="systemUsesLightTheme"/> are the
    /// DWORDs under Themes\Personalize, or null when missing or not a DWORD. Only 0 and 1 count:
    /// anything else means light apps and a dark taskbar, as Windows 10 before 1903 has.
    /// </summary>
    public static Theme Decide(uint? appsUseLightTheme, uint? systemUsesLightTheme, bool isHighContrast) =>
        new(IsAppDark: appsUseLightTheme == 0, IsTaskbarDark: systemUsesLightTheme != 1, isHighContrast);

    /// <summary>The settings window uses the dark palette (high contrast uses system colors instead).</summary>
    public bool IsWindowDark => IsAppDark && !IsHighContrast;
}

/// <summary>Which DWM attributes the settings window sets on a given Windows build (REQ-013-07).</summary>
internal readonly record struct WindowEffects(int DarkModeAttribute, bool RoundCorners, bool MicaTitleBar)
{
    public const int DwmDarkModeAttribute = 20;          // DWMWA_USE_IMMERSIVE_DARK_MODE, build 18985+
    public const int DwmDarkModeAttributeBefore20H1 = 19; // the same attribute under its old number
    public const int NoAttribute = 0;

    const int FirstDarkTitleBarBuild = 17763;
    const int FirstCurrentDarkModeBuild = 18985;
    const int FirstMicaBuild = 22621;

    /// <summary>
    /// Corners are always requested: Windows 10 ignores the call and keeps its square frame. Mica
    /// is only requested where <c>DWMSBT_MAINWINDOW</c> exists.
    /// </summary>
    public static WindowEffects ForBuild(int build) => new(
        build >= FirstCurrentDarkModeBuild ? DwmDarkModeAttribute
            : build >= FirstDarkTitleBarBuild ? DwmDarkModeAttributeBefore20H1
            : NoAttribute,
        RoundCorners: true,
        MicaTitleBar: build >= FirstMicaBuild);
}

/// <summary>The dark tray menu needs undocumented uxtheme ordinals, only known from build 18362 (REQ-013-22).</summary>
internal static class MenuDarkMode
{
    const int FirstSupportedBuild = 18362;

    public static bool IsSupported(int build) => build >= FirstSupportedBuild;
}
