using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class ThemeTests
{
    [Theory] // AC-013-01: null stands for a missing value or one of the wrong type
    [InlineData(0u, true)]
    [InlineData(1u, false)]
    [InlineData(2u, false)]
    [InlineData(null, false)]
    public void AppsValue_OnlyZeroMeansDark(uint? appsUseLightTheme, bool isAppDark)
    {
        Assert.Equal(isAppDark, Theme.Decide(appsUseLightTheme, 1, false).IsAppDark);
    }

    [Theory] // AC-013-01
    [InlineData(0u, true)]
    [InlineData(1u, false)]
    [InlineData(2u, true)]
    [InlineData(null, true)]
    public void TaskbarValue_OnlyOneMeansLight(uint? systemUsesLightTheme, bool isTaskbarDark)
    {
        Assert.Equal(isTaskbarDark, Theme.Decide(1, systemUsesLightTheme, false).IsTaskbarDark);
    }

    [Fact] // AC-013-01
    public void Values_AreIndependent()
    {
        Theme theme = Theme.Decide(0, 1, false);

        Assert.True(theme.IsAppDark);
        Assert.False(theme.IsTaskbarDark);
        Assert.False(theme.IsHighContrast);
    }

    [Fact] // AC-013-01, REQ-013-04
    public void HighContrast_OverridesTheDarkWindow()
    {
        Theme theme = Theme.Decide(0, 0, true);

        Assert.True(theme.IsHighContrast);
        Assert.False(theme.IsWindowDark);
        Assert.True(Theme.Decide(0, 0, false).IsWindowDark);
    }

    [Fact]
    public void Default_IsLightAppsOnADarkTaskbar()
    {
        Assert.Equal(new Theme(false, true, false), Theme.Default);
    }

    [Theory] // AC-013-03
    [InlineData(19045, WindowEffects.DwmDarkModeAttribute, false)]
    [InlineData(22000, WindowEffects.DwmDarkModeAttribute, false)]
    [InlineData(22621, WindowEffects.DwmDarkModeAttribute, true)]
    [InlineData(26100, WindowEffects.DwmDarkModeAttribute, true)]
    [InlineData(18363, WindowEffects.DwmDarkModeAttributeBefore20H1, false)]
    [InlineData(17134, WindowEffects.NoAttribute, false)]
    public void WindowEffects_DependOnTheBuild(int build, int darkModeAttribute, bool mica)
    {
        WindowEffects effects = WindowEffects.ForBuild(build);

        Assert.Equal(darkModeAttribute, effects.DarkModeAttribute);
        Assert.True(effects.RoundCorners);
        Assert.Equal(mica, effects.MicaTitleBar);
    }

    [Theory] // AC-013-08
    [InlineData(17763, false)]
    [InlineData(18361, false)]
    [InlineData(18362, true)]
    [InlineData(26100, true)]
    public void DarkMenu_OnlyWhereTheOrdinalsAreKnown(int build, bool isSupported)
    {
        Assert.Equal(isSupported, MenuDarkMode.IsSupported(build));
    }
}
