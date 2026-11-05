using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

/// <summary>Spec 006, AC-006-01 to AC-006-10.</summary>
public class LowBatteryAlertTests
{
    LowBatteryAlert _alert;

    BatteryNotice On(int percent, ChargeState charge = ChargeState.NotCharging) =>
        _alert.Evaluate(HeadsetPower.On, (byte)percent, charge, isChargeKnown: true);

    BatteryNotice[] Discharge(params int[] percents) => percents.Select(p => On(p)).ToArray();

    static int Count(BatteryNotice[] notices, BatteryNotice notice) => notices.Count(n => n == notice);

    [Fact]
    public void SameStateTwice_NotifiesOnce() // AC-006-01
    {
        Assert.Equal(BatteryNotice.Low, On(15));
        Assert.Equal(BatteryNotice.None, On(15));
    }

    [Fact]
    public void Low_FiresUnderTheLimit() // AC-006-02
    {
        Assert.Equal([BatteryNotice.None, BatteryNotice.None, BatteryNotice.Low], Discharge(21, 20, 19));
    }

    [Fact]
    public void Wavering_UnderTheMargin_DoesNotRepeat() // AC-006-03
    {
        Assert.Equal(1, Count(Discharge(19, 20, 19, 24, 19), BatteryNotice.Low));
    }

    [Fact]
    public void RecoveringToTheMargin_Rearms() // AC-006-04
    {
        Assert.Equal(2, Count(Discharge(19, 25, 19), BatteryNotice.Low));
    }

    [Theory]
    [InlineData(ChargeState.Charging)]
    [InlineData(ChargeState.Full)]
    internal void Charging_Rearms(ChargeState charge) // AC-006-05
    {
        Assert.Equal(BatteryNotice.Low, On(19));
        Assert.Equal(BatteryNotice.None, On(19, charge));
        Assert.Equal(BatteryNotice.Low, On(19));
    }

    [Fact]
    public void Discharging_FiresLowThenCritical() // AC-006-06
    {
        Assert.Equal([BatteryNotice.None, BatteryNotice.Low, BatteryNotice.Critical], Discharge(21, 15, 9));
    }

    [Fact]
    public void StartingCritical_ShowsOnlyTheCriticalNotice() // AC-006-07
    {
        Assert.Equal([BatteryNotice.Critical, BatteryNotice.None, BatteryNotice.None], Discharge(8, 7, 5));
    }

    [Fact]
    public void CriticalRearms_AtItsOwnMargin()
    {
        Assert.Equal([BatteryNotice.Critical, BatteryNotice.None, BatteryNotice.Critical], Discharge(9, 15, 9));
    }

    [Theory]
    [InlineData(HeadsetPower.Off)]
    [InlineData(HeadsetPower.NoDongle)]
    internal void TurningOffAndOn_DoesNotRearm(HeadsetPower gone) // AC-006-08
    {
        Assert.Equal(BatteryNotice.Low, On(15));
        Assert.Equal(BatteryNotice.None, _alert.Evaluate(gone, 0, ChargeState.NotCharging, isChargeKnown: false));
        Assert.Equal(BatteryNotice.None, _alert.Evaluate(HeadsetPower.Checking, 0, ChargeState.NotCharging, isChargeKnown: false));
        Assert.Equal(BatteryNotice.None, On(15));
    }

    [Theory]
    [InlineData(HeadsetPower.On, ChargeState.Charging)]
    [InlineData(HeadsetPower.On, ChargeState.Full)]
    [InlineData(HeadsetPower.Off, ChargeState.NotCharging)]
    [InlineData(HeadsetPower.Checking, ChargeState.NotCharging)]
    [InlineData(HeadsetPower.NoDongle, ChargeState.NotCharging)]
    internal void NotDischarging_NeverFires(HeadsetPower power, ChargeState charge) // AC-006-09
    {
        Assert.Equal(BatteryNotice.None, _alert.Evaluate(power, 5, charge, isChargeKnown: true));
    }

    [Fact]
    public void ChargingError_CountsAsDischarging() // AC-006-10
    {
        Assert.Equal(BatteryNotice.Low, On(15, ChargeState.Error));
    }

    [Fact]
    public void UnknownCharge_WaitsForTheChargingResponse()
    {
        Assert.Equal(BatteryNotice.None, _alert.Evaluate(HeadsetPower.On, 15, ChargeState.NotCharging, isChargeKnown: false));
        Assert.Equal(BatteryNotice.None, On(15, ChargeState.Charging));
    }

    [Fact]
    public void UnknownCharge_FiresOnceItIsKnown()
    {
        Assert.Equal(BatteryNotice.None, _alert.Evaluate(HeadsetPower.On, 15, ChargeState.NotCharging, isChargeKnown: false));
        Assert.Equal(BatteryNotice.Low, On(15));
    }

    [Fact]
    public void Limits_AreTheSpecValues()
    {
        Assert.Equal(20, LowBatteryAlert.LowLimit);
        Assert.Equal(IconDescription.LowLimit, LowBatteryAlert.LowLimit);
        Assert.Equal(10, LowBatteryAlert.CriticalLimit);
        Assert.Equal(5, LowBatteryAlert.RearmMargin);
    }
}
