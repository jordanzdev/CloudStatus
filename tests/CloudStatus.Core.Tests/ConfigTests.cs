using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

/// <summary>Spec 007: defaults, ranges and fallbacks of the stored settings.</summary>
public class ConfigTests
{
    [Fact]
    public void NoStoredValues_GiveTheDefaults() // AC-007-01
    {
        Config config = Config.FromStored(null, null, null, null);

        Assert.Equal(Config.Default, config);
        Assert.True(config.LowBatteryNotice);
        Assert.Equal(20, config.LowBatteryPercent);
        Assert.Equal(5u, config.PollMinutes);
        Assert.Equal(UiLanguage.Auto, config.Language);
    }

    [Theory]
    [InlineData(14u, 20)]
    [InlineData(15u, 15)]
    [InlineData(45u, 45)]
    [InlineData(46u, 20)]
    [InlineData(0u, 20)]
    [InlineData(uint.MaxValue, 20)]
    public void LowBatteryPercent_KeepsOnlyTheRange(uint stored, int expected) // AC-007-02
    {
        Assert.Equal(expected, Config.FromStored(null, stored, null, null).LowBatteryPercent);
    }

    [Theory]
    [InlineData(0u, 5u)]
    [InlineData(1u, 1u)]
    [InlineData(30u, 30u)]
    [InlineData(31u, 5u)]
    public void PollMinutes_KeepsOnlyTheRange(uint stored, uint expected) // AC-007-02
    {
        Assert.Equal(expected, Config.FromStored(null, null, stored, null).PollMinutes);
    }

    [Fact]
    public void InvalidValues_FallBackAlone() // AC-007-03
    {
        Config config = Config.FromStored(lowBatteryNotice: 2, lowBatteryPercent: 30, pollMinutes: 10, language: 3);

        Assert.True(config.LowBatteryNotice);
        Assert.Equal(UiLanguage.Auto, config.Language);
        Assert.Equal(30, config.LowBatteryPercent);
        Assert.Equal(10u, config.PollMinutes);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(1u, true)]
    public void Notice_ReadsZeroAndOne(uint stored, bool expected)
    {
        Assert.Equal(expected, Config.FromStored(stored, null, null, null).LowBatteryNotice);
    }

    [Theory]
    [InlineData(0u, UiLanguage.Auto)]
    [InlineData(1u, UiLanguage.Spanish)]
    [InlineData(2u, UiLanguage.English)]
    internal void Language_MatchesCursorLockNumbers(uint stored, UiLanguage expected)
    {
        Assert.Equal(expected, Config.FromStored(null, null, null, stored).Language);
    }

    [Fact]
    public void Range_StaysBetweenCriticalAndAmber() // REQ-007-02
    {
        Assert.True(Config.MinLowBatteryPercent > LowBatteryAlert.CriticalLimit);
        Assert.True(Config.MaxLowBatteryPercent < IconDescription.MediumLimit);
    }

    [Fact]
    public void PollInterval_IsInMilliseconds()
    {
        Assert.Equal(5u * 60 * 1000, Config.Default.PollIntervalMs);
        Assert.Equal(30u * 60 * 1000, (Config.Default with { PollMinutes = 30 }).PollIntervalMs);
    }
}
