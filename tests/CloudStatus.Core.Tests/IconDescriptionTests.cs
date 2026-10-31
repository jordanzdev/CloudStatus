using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class IconDescriptionTests
{
    const uint Teal = 0x26C6A6, Dark = 0x2B313D, Amber = 0xF9A825, Red = 0xE53935, Grey = 0x6B7280;

    static IconDescription On(byte percent, ChargeState charge = ChargeState.NotCharging) =>
        IconDescription.Describe(HeadsetPower.On, percent, charge);

    [Theory]
    [InlineData(0, Red)]
    [InlineData(19, Red)]
    [InlineData(20, Amber)]
    [InlineData(49, Amber)]
    [InlineData(50, Dark)]
    [InlineData(100, Dark)]
    public void Battery_PicksTheBackgroundByLevel(int percent, uint background)
    {
        IconDescription description = On((byte)percent);

        Assert.Equal(percent, description.Number);
        Assert.Equal(background, description.Background);
        Assert.False(description.IsHollow);
    }

    [Theory]
    [InlineData(ChargeState.Charging)]
    [InlineData(ChargeState.Full)]
    internal void Charging_WinsOverALowLevel(ChargeState charge)
    {
        IconDescription description = On(10, charge);

        Assert.Equal(Teal, description.Background);
        Assert.Equal(10, description.Number);
    }

    [Fact]
    public void ChargingError_IsRedAtAnyLevel()
    {
        Assert.Equal(Red, On(90, ChargeState.Error).Background);
    }

    [Theory]
    [InlineData(HeadsetPower.Checking, Dark, false)]
    [InlineData(HeadsetPower.Off, Grey, false)]
    [InlineData(HeadsetPower.NoDongle, Grey, true)]
    internal void OtherStates_ShowNoNumber(HeadsetPower power, uint background, bool isHollow)
    {
        IconDescription description = IconDescription.Describe(power, 87, ChargeState.NotCharging);

        Assert.Equal(IconDescription.NoNumber, description.Number);
        Assert.Equal(background, description.Background);
        Assert.Equal(isHollow, description.IsHollow);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(80)]
    public void Text_IsReadableOnEveryBatteryBackground(int percent)
    {
        IconDescription description = On((byte)percent);
        Assert.True(IconDescription.ContrastRatio(description.Background, description.TextColor) >= 4.5f);
    }

    [Theory]
    [InlineData(ChargeState.Charging)]
    [InlineData(ChargeState.Error)]
    internal void Text_IsReadableWhileCharging(ChargeState charge)
    {
        IconDescription description = On(50, charge);
        Assert.True(IconDescription.ContrastRatio(description.Background, description.TextColor) >= 4.5f);
    }

    [Fact]
    public void ContrastRatio_MatchesKnownValues()
    {
        Assert.Equal(21f, IconDescription.ContrastRatio(0x000000, 0xFFFFFF), 2);
        Assert.Equal(1f, IconDescription.ContrastRatio(0x123456, 0x123456), 2);
    }

    [Fact]
    public void SameStatus_GivesAnEqualDescription()
    {
        Assert.Equal(On(87), On(87));
        Assert.NotEqual(On(87), On(86));
    }
}
