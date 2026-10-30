using CloudStatus.Core;
using static CloudStatus.Native.User32;

namespace CloudStatus.Device;

/// <summary>
/// Connects <see cref="HidTransport"/> to <see cref="HeadsetMonitor"/>: reports and timers go in,
/// and the actions that come out become queries and timers. One command at a time.
/// </summary>
internal static class HeadsetLink
{
    public const nuint DeadlineTimerId = 3;
    public const nuint PollTimerId = 4;

    const uint DeadlineMs = 500;
    const uint PollIntervalMs = 5 * 60 * 1000;
    const uint PollToleranceMs = 30 * 1000; // lets Windows group the wake-up with others

    static nint s_window;
    static HeadsetMonitor s_monitor;
    static CommandTracker s_tracker;

    /// <summary>Raised after anything that may change what is shown.</summary>
    public static event Action? Changed;

    public static HeadsetPower Power => s_monitor.Power;

    public static byte BatteryPercent => s_monitor.BatteryPercent;

    public static ChargeState Charge => s_monitor.Charge;

    public static void Initialize(nint window)
    {
        s_window = window;
        HidTransport.ReportReceived += OnReport;
        HidTransport.Failed += _ => OnConnectionLost();
    }

    public static void Connect(in HidCandidate target)
    {
        Disconnect();
        if (HidTransport.TryOpen(target))
            Apply(s_monitor.OnDongleConnected());
    }

    public static void Disconnect()
    {
        OnConnectionLost();
        HidTransport.Close();
    }

    /// <summary>
    /// Modal loops (menus, message boxes) do not run the app's loop but do dispatch timers, so
    /// completions are checked first: a response that arrived in time is never a timeout.
    /// </summary>
    public static void OnDeadlineTimer()
    {
        StopDeadline();
        HidTransport.Pump();
        if (!s_tracker.TryTimeOut())
            return;

        HidTransport.CancelWrite();
        Apply(s_monitor.OnTimeout());
    }

    public static void OnPollTimer() => Apply(s_monitor.OnPollDue());

    static void OnReport(ReadOnlySpan<byte> report)
    {
        if (!HpReports.TryGetCommand(report, out byte command))
            return;

        if (s_tracker.TryComplete(command, Environment.TickCount64, out _))
            StopDeadline();

        if (HpReports.TryParseBattery(report, out byte percent))
            Apply(s_monitor.OnBattery(percent));
        else if (HpReports.TryParseCharging(report, out ChargeState charge))
            Apply(s_monitor.OnCharging(charge));
        else if (HpReports.TryParsePower(report, out bool isOn))
            Apply(s_monitor.OnPower(isOn));
    }

    static void OnConnectionLost()
    {
        StopDeadline();
        s_tracker.TryTimeOut();
        Apply(s_monitor.OnDongleLost());
    }

    static void Apply(MonitorAction action)
    {
        if ((action & MonitorAction.StartPolling) != 0)
            SetCoalescableTimer(s_window, PollTimerId, PollIntervalMs, 0, PollToleranceMs);
        if ((action & MonitorAction.StopPolling) != 0)
            KillTimer(s_window, PollTimerId);

        if ((action & MonitorAction.QueryBattery) != 0)
            Send(Commands.QueryBattery, HpReports.BatteryCommand);
        else if ((action & MonitorAction.QueryCharging) != 0)
            Send(Commands.QueryCharging, HpReports.ChargingCommand);

        Changed?.Invoke();
    }

    /// <summary>A command that cannot go out now counts as unanswered, so nothing waits for it.</summary>
    static void Send(byte[] report, byte command)
    {
        if (s_tracker.IsInFlight)
            return;

        if (HidTransport.TrySend(report) && s_tracker.TryStart(command, Environment.TickCount64))
            SetTimer(s_window, DeadlineTimerId, DeadlineMs, 0);
        else if (HidTransport.IsOpen)
            Apply(s_monitor.OnTimeout());
    }

    static void StopDeadline() => KillTimer(s_window, DeadlineTimerId);
}
