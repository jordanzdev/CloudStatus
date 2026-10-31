using CloudStatus.Core;
using CloudStatus.Device;
using Xunit;

namespace CloudStatus.Core.Tests;

public class ProtocolTests
{
    static byte[] Battery(byte percent) =>
        [0x06, 0xFF, 0xBB, 0x02, 0x00, 0x0F, 0xA0, percent, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    [Fact]
    public void Commands_AreExactlyTheAllowedSet()
    {
        Assert.Collection(Commands.All,
            battery => Assert.Equal(new byte[] { 0x06, 0xFF, 0xBB, 0x02, 0x00 }, battery),
            charging => Assert.Equal(new byte[] { 0x06, 0xFF, 0xBB, 0x03, 0x00 }, charging));
    }

    // Observed on the target hardware (docs/protocol.md, Push events).
    [Theory]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x03, 0x01, 0x00 }, ChargeState.Charging)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x03, 0x00, 0x00 }, ChargeState.NotCharging)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x03, 0x02, 0x00 }, ChargeState.Full)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x03, 0x07, 0x00 }, ChargeState.Error)]
    internal void TryParseCharging_ReadsTheState(byte[] report, ChargeState expected)
    {
        Assert.True(HpReports.TryParseCharging(report, out ChargeState state));
        Assert.Equal(expected, state);
    }

    [Theory]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x01, 0x01, 0x00 }, true)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x01, 0x04, 0x00 }, true)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x01, 0x03, 0x00 }, false)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x01, 0x02, 0x00 }, false)]
    [InlineData(new byte[] { 0x06, 0xFF, 0xBB, 0x01, 0x63, 0x00 }, false)]
    public void TryParsePower_TreatsEverythingButOnAsOff(byte[] report, bool expected)
    {
        Assert.True(HpReports.TryParsePower(report, out bool isOn));
        Assert.Equal(expected, isOn);
    }

    [Fact]
    public void Parsers_IgnoreOtherCommandsAndShortReports()
    {
        byte[] mute = [0x06, 0xFF, 0xBB, 0x20, 0x01, 0x00];

        Assert.False(HpReports.TryParseCharging(mute, out _));
        Assert.False(HpReports.TryParsePower(mute, out _));
        Assert.False(HpReports.TryParseBattery(mute, out _));
        Assert.False(HpReports.TryParsePower(new byte[] { 0x06, 0xFF, 0xBB, 0x01 }, out _));
        Assert.False(HpReports.TryParseCharging(new byte[] { 0x06, 0xFF, 0xBB, 0x03 }, out _));
    }

    [Fact]
    public void Commands_NeverIncludeTheCommandThatUnmutes()
    {
        Assert.All(Commands.All, report => Assert.NotEqual(0x01, report[3]));
    }

    [Fact]
    public void TryPad_FillsTheOutputLengthWithZeros()
    {
        var destination = new byte[20];
        destination.AsSpan().Fill(0xAA);

        Assert.True(HpReports.TryPad(Commands.QueryBattery, destination));
        Assert.Equal(Commands.QueryBattery, destination[..5]);
        Assert.All(destination[5..], value => Assert.Equal(0, value));
    }

    [Fact]
    public void TryPad_RefusesAReportThatDoesNotFit()
    {
        Assert.False(HpReports.TryPad(Commands.QueryBattery, new byte[4]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(87)]
    [InlineData(100)]
    public void TryParseBattery_ReadsByteSeven(int percent)
    {
        Assert.True(HpReports.TryParseBattery(Battery((byte)percent), out byte parsed));
        Assert.Equal(percent, parsed);
    }

    [Fact]
    public void TryParseBattery_RejectsAPercentAbove100()
    {
        Assert.False(HpReports.TryParseBattery(Battery(101), out _));
    }

    [Fact]
    public void TryParseBattery_RejectsAShortReport()
    {
        Assert.False(HpReports.TryParseBattery(Battery(50).AsSpan(0, 7), out _));
        Assert.False(HpReports.TryParseBattery([], out _));
    }

    [Theory]
    [InlineData(0, 0x07)]
    [InlineData(1, 0x00)]
    [InlineData(2, 0x00)]
    [InlineData(3, 0x03)]
    public void TryParseBattery_RejectsAWrongHeaderOrCommand(int index, int value)
    {
        byte[] report = Battery(50);
        report[index] = (byte)value;
        Assert.False(HpReports.TryParseBattery(report, out _));
    }

    [Fact]
    public void Tracker_RefusesASecondCommand()
    {
        CommandTracker tracker = default;
        Assert.True(tracker.TryStart(2, 0));
        Assert.False(tracker.TryStart(3, 1));
    }

    [Fact]
    public void Tracker_CompletesWithTheElapsedTime()
    {
        CommandTracker tracker = default;
        tracker.TryStart(2, 1000);

        Assert.True(tracker.TryComplete(2, 1034, out long elapsed));
        Assert.Equal(34, elapsed);
        Assert.False(tracker.IsInFlight);
    }

    [Fact]
    public void Tracker_IgnoresAnotherCommandsResponse()
    {
        CommandTracker tracker = default;
        tracker.TryStart(2, 0);

        Assert.False(tracker.TryComplete(3, 10, out _));
        Assert.True(tracker.IsInFlight);
    }

    [Fact]
    public void Tracker_AllowsANewCommandAfterATimeout()
    {
        CommandTracker tracker = default;
        tracker.TryStart(2, 0);

        Assert.True(tracker.TryTimeOut());
        Assert.False(tracker.TryComplete(2, 600, out _));
        Assert.True(tracker.TryStart(2, 700));
    }

    [Fact]
    public void Tracker_HasNothingToTimeOutWhenIdle()
    {
        CommandTracker tracker = default;
        Assert.False(tracker.TryTimeOut());
    }
}
