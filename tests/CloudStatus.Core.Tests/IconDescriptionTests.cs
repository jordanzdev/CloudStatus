using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class IconDescriptionTests
{
    const uint Teal = 0x26C6A6, Dark = 0x2B313D, Amber = 0xF9A825, Red = 0xE53935, Grey = 0x6B7280;
    const uint White = 0xFFFFFF, NearBlack = 0x1A1A1A;
    const uint Windows11DarkTaskbar = 0x1C1C1C, Windows11LightTaskbar = 0xEEEEEE;

    static readonly Theme DarkTaskbar = Theme.Decide(1, 0, false);
    static readonly Theme LightTaskbar = Theme.Decide(1, 1, false);
    static readonly Theme HighContrast = Theme.Decide(1, 0, true);

    static IconDescription On(byte percent, ChargeState charge = ChargeState.NotCharging, Theme? theme = null) =>
        IconDescription.Describe(HeadsetPower.On, percent, charge, IconDescription.DefaultLowLimit, theme ?? DarkTaskbar);

    static IconDescription Other(HeadsetPower power, Theme theme) =>
        IconDescription.Describe(power, 87, ChargeState.NotCharging, IconDescription.DefaultLowLimit, theme);

    [Theory] // AC-013-04
    [InlineData(0, Red)]
    [InlineData(19, Red)]
    [InlineData(20, Amber)]
    [InlineData(49, Amber)]
    public void AttentionLevels_AreNumbersOnAPlate(int percent, uint plate)
    {
        IconDescription description = On((byte)percent);

        Assert.Equal(percent, description.Number);
        Assert.Equal(plate, description.Color);
        Assert.Equal(IconShape.Plate, description.Shape);
    }

    [Theory] // AC-013-04
    [InlineData(50, false, White)]
    [InlineData(100, false, White)]
    [InlineData(50, true, NearBlack)]
    [InlineData(100, true, NearBlack)]
    public void HealthyBattery_IsAPlainNumberInTheTaskbarColor(int percent, bool isTaskbarLight, uint color)
    {
        IconDescription description = On((byte)percent, theme: isTaskbarLight ? LightTaskbar : DarkTaskbar);

        Assert.Equal(percent, description.Number);
        Assert.Equal(IconShape.Number, description.Shape);
        Assert.Equal(color, description.Color);
        Assert.Equal(color, description.TextColor);
    }

    [Theory]
    [InlineData(ChargeState.Charging)]
    [InlineData(ChargeState.Full)]
    internal void Charging_WinsOverALowLevel(ChargeState charge)
    {
        IconDescription description = On(10, charge);

        Assert.Equal(Teal, description.Color);
        Assert.Equal(IconShape.Plate, description.Shape);
        Assert.Equal(10, description.Number);
    }

    [Fact]
    public void ChargingError_IsRedAtAnyLevel()
    {
        Assert.Equal(Red, On(90, ChargeState.Error).Color);
        Assert.Equal(IconShape.Plate, On(90, ChargeState.Error).Shape);
    }

    [Theory] // AC-013-04
    [InlineData(HeadsetPower.Checking, IconShape.Hollow)]
    [InlineData(HeadsetPower.Off, IconShape.Headset)]
    [InlineData(HeadsetPower.NoDongle, IconShape.HeadsetSlashed)]
    internal void OtherStates_AreGlyphsInTheTaskbarColor(HeadsetPower power, IconShape shape)
    {
        IconDescription dark = Other(power, DarkTaskbar);
        IconDescription light = Other(power, LightTaskbar);

        Assert.Equal(IconDescription.NoNumber, dark.Number);
        Assert.Equal(shape, dark.Shape);
        Assert.Equal(White, dark.Color);
        Assert.Equal(shape, light.Shape);
        Assert.Equal(NearBlack, light.Color);
    }

    [Theory] // REQ-013-04: high contrast keeps spec 005's plates
    [InlineData(HeadsetPower.Checking, Dark, IconShape.Plate)]
    [InlineData(HeadsetPower.Off, Grey, IconShape.Plate)]
    [InlineData(HeadsetPower.NoDongle, Grey, IconShape.Hollow)]
    internal void HighContrast_UsesPlatesForEveryState(HeadsetPower power, uint color, IconShape shape)
    {
        IconDescription description = Other(power, HighContrast);

        Assert.Equal(IconDescription.NoNumber, description.Number);
        Assert.Equal(color, description.Color);
        Assert.Equal(shape, description.Shape);
    }

    [Fact]
    public void HighContrast_KeepsTheDarkPlateForAHealthyBattery()
    {
        IconDescription description = On(80, theme: HighContrast);

        Assert.Equal(Dark, description.Color);
        Assert.Equal(IconShape.Plate, description.Shape);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(80)]
    public void Text_IsReadableOnEveryPlate(int percent)
    {
        IconDescription description = On((byte)percent, theme: HighContrast);
        Assert.True(IconDescription.ContrastRatio(description.Color, description.TextColor) >= 4.5f);
    }

    [Theory]
    [InlineData(ChargeState.Charging)]
    [InlineData(ChargeState.Error)]
    internal void Text_IsReadableWhileCharging(ChargeState charge)
    {
        IconDescription description = On(50, charge);
        Assert.True(IconDescription.ContrastRatio(description.Color, description.TextColor) >= 4.5f);
    }

    [Fact] // AC-013-06
    public void TaskbarForeground_IsReadableOnTheWindows11Taskbar()
    {
        Assert.True(IconDescription.ContrastRatio(IconDescription.DarkTaskbarForeground, Windows11DarkTaskbar) >= 4.5f);
        Assert.True(IconDescription.ContrastRatio(IconDescription.LightTaskbarForeground, Windows11LightTaskbar) >= 4.5f);
    }

    [Fact]
    public void ContrastRatio_MatchesKnownValues()
    {
        Assert.Equal(21f, IconDescription.ContrastRatio(0x000000, 0xFFFFFF), 2);
        Assert.Equal(1f, IconDescription.ContrastRatio(0x123456, 0x123456), 2);
    }

    [Fact] // AC-013-05
    public void SameStatusAndTheme_GivesAnEqualDescription()
    {
        Assert.Equal(On(87), On(87));
        Assert.NotEqual(On(87), On(86));
    }

    [Theory] // AC-013-05
    [InlineData(HeadsetPower.On, 80, true)]
    [InlineData(HeadsetPower.On, 30, false)]
    [InlineData(HeadsetPower.On, 10, false)]
    [InlineData(HeadsetPower.Checking, 0, true)]
    [InlineData(HeadsetPower.Off, 0, true)]
    [InlineData(HeadsetPower.NoDongle, 0, true)]
    internal void TaskbarTheme_OnlyChangesStatesDrawnInItsColor(HeadsetPower power, int percent, bool changes)
    {
        IconDescription dark = IconDescription.Describe(power, (byte)percent, ChargeState.NotCharging, IconDescription.DefaultLowLimit, DarkTaskbar);
        IconDescription light = IconDescription.Describe(power, (byte)percent, ChargeState.NotCharging, IconDescription.DefaultLowLimit, LightTaskbar);

        Assert.Equal(changes, dark != light);
    }

    [Theory]
    [InlineData(29, Red)]
    [InlineData(30, Amber)]
    public void ConfiguredLowLimit_MovesTheRedBoundary(int percent, uint plate) // AC-007-04
    {
        Assert.Equal(plate, IconDescription.Describe(HeadsetPower.On, (byte)percent, ChargeState.NotCharging, 30, DarkTaskbar).Color);
    }
}
