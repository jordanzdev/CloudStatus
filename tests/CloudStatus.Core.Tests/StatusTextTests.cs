using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class StatusTextTests
{
    // object[] rows: the enums are internal, so TheoryData<…> cannot be public.
    public static IEnumerable<object[]> EveryState()
    {
        foreach (bool isEnglish in new[] { false, true })
            foreach (HeadsetPower power in Enum.GetValues<HeadsetPower>())
                foreach (ChargeState charge in Enum.GetValues<ChargeState>())
                    yield return [isEnglish, power, (byte)100, charge];
    }

    [Theory] // AC-013-07
    [MemberData(nameof(EveryState))]
    internal void Tooltip_HasTwoLinesAndFits(bool isEnglish, HeadsetPower power, byte percent, ChargeState charge)
    {
        string tooltip = StatusText.Tooltip(isEnglish, power, percent, charge);
        string[] lines = tooltip.Split('\n');

        Assert.True(tooltip.Length < StatusText.TooltipCapacity); // room for the null
        Assert.Equal(2, lines.Length);
        Assert.Equal("CloudStatus", lines[0]);
        Assert.Equal(StatusText.Line(isEnglish, power, percent, charge), lines[1]);
        Assert.NotEmpty(lines[1]);
    }

    [Theory] // REQ-013-20
    [InlineData(false, ChargeState.Charging, "78 % · Cargando")]
    [InlineData(true, ChargeState.Charging, "78 % · Charging")]
    [InlineData(true, ChargeState.Full, "78 % · Full")]
    [InlineData(false, ChargeState.Error, "78 % · Error de carga")]
    [InlineData(true, ChargeState.NotCharging, "78 %")]
    internal void BatteryLine_GivesPercentAndCharge(bool isEnglish, ChargeState charge, string expected)
    {
        Assert.Equal(expected, StatusText.Line(isEnglish, HeadsetPower.On, 78, charge));
    }

    [Theory]
    [InlineData(false, HeadsetPower.Off, "Cascos apagados")]
    [InlineData(true, HeadsetPower.Off, "Headset off")]
    [InlineData(false, HeadsetPower.NoDongle, "Sin dongle")]
    [InlineData(true, HeadsetPower.NoDongle, "No dongle")]
    [InlineData(true, HeadsetPower.Checking, "Dongle connected")]
    internal void OtherLines_NameTheState(bool isEnglish, HeadsetPower power, string expected)
    {
        Assert.Equal(expected, StatusText.Line(isEnglish, power, 0, ChargeState.NotCharging));
    }
}
