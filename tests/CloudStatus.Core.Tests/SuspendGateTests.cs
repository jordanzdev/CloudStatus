using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

/// <summary>Spec 008, AC-008-01 to AC-008-06.</summary>
public class SuspendGateTests
{
    SuspendGate _gate;

    [Fact]
    public void StartsAwake()
    {
        Assert.False(_gate.IsSuspended);
        Assert.True(_gate.AcceptsDeviceEvents);
    }

    [Fact]
    public void Suspend_Disconnects() // AC-008-01
    {
        Assert.Equal(PowerAction.Disconnect, _gate.OnSuspend());
        Assert.True(_gate.IsSuspended);
    }

    [Fact]
    public void WhileSuspended_DeviceEventsAreIgnored() // AC-008-02
    {
        _gate.OnSuspend();
        Assert.False(_gate.AcceptsDeviceEvents);
    }

    [Fact]
    public void ResumeAutomatic_ReconnectsAndAcceptsEvents() // AC-008-03
    {
        _gate.OnSuspend();

        Assert.Equal(PowerAction.Reconnect, _gate.OnResumeAutomatic());
        Assert.False(_gate.IsSuspended);
        Assert.True(_gate.AcceptsDeviceEvents);
    }

    [Fact]
    public void ResumeSuspend_AfterResume_DoesNothing() // AC-008-04
    {
        _gate.OnSuspend();
        _gate.OnResumeAutomatic();

        Assert.Equal(PowerAction.None, _gate.OnResumeSuspend());
        Assert.True(_gate.AcceptsDeviceEvents);
    }

    [Fact]
    public void ResumeWithoutSuspend_StillReconnects() // AC-008-05
    {
        Assert.Equal(PowerAction.Reconnect, _gate.OnResumeAutomatic());
    }

    [Fact]
    public void SecondSuspend_DoesNothingMore() // AC-008-06
    {
        _gate.OnSuspend();

        Assert.Equal(PowerAction.None, _gate.OnSuspend());
        Assert.True(_gate.IsSuspended);
    }

    [Fact]
    public void ManyCycles_BehaveTheSame()
    {
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(PowerAction.Disconnect, _gate.OnSuspend());
            Assert.Equal(PowerAction.Reconnect, _gate.OnResumeAutomatic());
            Assert.Equal(PowerAction.None, _gate.OnResumeSuspend());
        }
    }
}
