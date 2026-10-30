using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class HeadsetMonitorTests
{
    static HeadsetMonitor Connected()
    {
        HeadsetMonitor monitor = default;
        monitor.OnDongleConnected();
        return monitor;
    }

    static HeadsetMonitor On(byte percent = 87)
    {
        HeadsetMonitor monitor = Connected();
        monitor.OnBattery(percent);
        monitor.OnCharging(ChargeState.NotCharging);
        return monitor;
    }

    static HeadsetMonitor Off()
    {
        HeadsetMonitor monitor = On();
        monitor.OnPower(false);
        return monitor;
    }

    [Fact]
    public void StartsWithNoDongle()
    {
        HeadsetMonitor monitor = default;
        Assert.Equal(HeadsetPower.NoDongle, monitor.Power);
    }

    [Fact]
    public void Connecting_AsksForBatteryOnly()
    {
        HeadsetMonitor monitor = default;
        Assert.Equal(MonitorAction.QueryBattery, monitor.OnDongleConnected());
        Assert.Equal(HeadsetPower.Checking, monitor.Power);
    }

    [Fact]
    public void Battery_WhileChecking_TurnsOnThenAsksForCharging()
    {
        HeadsetMonitor monitor = Connected();

        Assert.Equal(MonitorAction.QueryCharging | MonitorAction.StartPolling, monitor.OnBattery(87));
        Assert.Equal(HeadsetPower.On, monitor.Power);
        Assert.Equal(87, monitor.BatteryPercent);
    }

    [Fact]
    public void Timeout_WhileChecking_MeansOff()
    {
        HeadsetMonitor monitor = Connected();

        Assert.Equal(MonitorAction.StopPolling, monitor.OnTimeout());
        Assert.Equal(HeadsetPower.Off, monitor.Power);
    }

    [Fact]
    public void ZeroBattery_WhileChecking_MeansOff()
    {
        HeadsetMonitor monitor = Connected();

        Assert.Equal(MonitorAction.StopPolling, monitor.OnBattery(0));
        Assert.Equal(HeadsetPower.Off, monitor.Power);
    }

    [Fact]
    public void PowerOff_KeepsTheLastBatteryAndStopsPolling()
    {
        HeadsetMonitor monitor = On(64);

        Assert.Equal(MonitorAction.StopPolling, monitor.OnPower(false));
        Assert.Equal(HeadsetPower.Off, monitor.Power);
        Assert.Equal(64, monitor.BatteryPercent);
    }

    [Fact]
    public void PowerOn_WhileOff_Refreshes()
    {
        HeadsetMonitor monitor = Off();

        Assert.Equal(MonitorAction.QueryBattery, monitor.OnPower(true));
        Assert.Equal(HeadsetPower.Checking, monitor.Power);
    }

    [Fact]
    public void PowerOn_WhileOn_DoesNothing()
    {
        HeadsetMonitor monitor = On();
        Assert.Equal(MonitorAction.None, monitor.OnPower(true));
    }

    [Fact]
    public void Power_WithNoDongle_IsIgnored()
    {
        HeadsetMonitor monitor = default;
        Assert.Equal(MonitorAction.None, monitor.OnPower(true));
        Assert.Equal(HeadsetPower.NoDongle, monitor.Power);
    }

    [Fact]
    public void ChargingPush_UpdatesWithNoQuery()
    {
        HeadsetMonitor monitor = On();

        Assert.Equal(MonitorAction.None, monitor.OnCharging(ChargeState.Charging));
        Assert.Equal(ChargeState.Charging, monitor.Charge);
    }

    [Fact]
    public void Poll_AsksOnlyForBattery()
    {
        HeadsetMonitor monitor = On();
        Assert.Equal(MonitorAction.QueryBattery, monitor.OnPollDue());
    }

    [Fact]
    public void Poll_WhileOff_DoesNothing()
    {
        HeadsetMonitor monitor = Off();
        Assert.Equal(MonitorAction.None, monitor.OnPollDue());
    }

    [Fact]
    public void OnePollTimeout_ThenAnAnswer_StaysOn()
    {
        HeadsetMonitor monitor = On();

        monitor.OnPollDue();
        Assert.Equal(MonitorAction.None, monitor.OnTimeout());
        monitor.OnPollDue();
        monitor.OnBattery(86);
        monitor.OnPollDue();
        Assert.Equal(MonitorAction.None, monitor.OnTimeout());

        Assert.Equal(HeadsetPower.On, monitor.Power);
        Assert.Equal(86, monitor.BatteryPercent);
    }

    [Fact]
    public void TwoPollTimeoutsInARow_MeanOff()
    {
        HeadsetMonitor monitor = On();

        monitor.OnPollDue();
        monitor.OnTimeout();
        monitor.OnPollDue();

        Assert.Equal(MonitorAction.StopPolling, monitor.OnTimeout());
        Assert.Equal(HeadsetPower.Off, monitor.Power);
    }

    [Fact]
    public void ChargingQueryTimeout_DoesNotCountAsAFailedPoll()
    {
        HeadsetMonitor monitor = Connected();
        monitor.OnBattery(87);

        Assert.Equal(MonitorAction.None, monitor.OnTimeout());
        Assert.Equal(HeadsetPower.On, monitor.Power);
        monitor.OnPollDue();
        Assert.Equal(MonitorAction.None, monitor.OnTimeout());
        Assert.Equal(HeadsetPower.On, monitor.Power);
    }

    [Fact]
    public void AnotherProgramsResponse_UpdatesTheBattery()
    {
        HeadsetMonitor monitor = On();

        Assert.Equal(MonitorAction.None, monitor.OnBattery(50));
        Assert.Equal(50, monitor.BatteryPercent);
    }

    [Fact]
    public void Lost_ForgetsEverythingAndStopsPolling()
    {
        HeadsetMonitor monitor = On();

        Assert.Equal(MonitorAction.StopPolling, monitor.OnDongleLost());
        Assert.Equal(HeadsetPower.NoDongle, monitor.Power);
        Assert.Equal(0, monitor.BatteryPercent);
    }
}
