using CloudStatus.Core;
using static CloudStatus.Native.User32;

namespace CloudStatus.Device;

internal enum LinkState
{
    Closed,
    Waiting,
    BatteryKnown,
    NoResponse,
    Failed,
}

/// <summary>Talks to the dongle through <see cref="HidTransport"/>: one battery query per connection.</summary>
internal static class HeadsetLink
{
    public const nuint DeadlineTimerId = 3;

    const uint DeadlineMs = 500;
    const long LatencyUnknown = -1;

    static nint s_window;
    static CommandTracker s_tracker;

    /// <summary>Raised when any of the values below changes.</summary>
    public static event Action? Changed;

    public static LinkState State { get; private set; }

    public static byte BatteryPercent { get; private set; }

    /// <summary>Milliseconds from the write to its response, or a negative value if it is not known.</summary>
    public static long LatencyMs { get; private set; } = LatencyUnknown;

    /// <summary>Win32 error when <see cref="State"/> is <see cref="LinkState.Failed"/>.</summary>
    public static int LastError { get; private set; }

    public static void Initialize(nint window)
    {
        s_window = window;
        HidTransport.ReportReceived += OnReport;
        HidTransport.Failed += OnFailed;
    }

    public static void Connect(in HidCandidate target)
    {
        Disconnect();
        if (!HidTransport.TryOpen(target))
        {
            SetState(LinkState.Failed);
            return;
        }

        LatencyMs = LatencyUnknown;
        SetState(LinkState.Waiting);
        if (HidTransport.TrySend(Commands.QueryBattery) && s_tracker.TryStart(HpReports.BatteryCommand, Environment.TickCount64))
            SetTimer(s_window, DeadlineTimerId, DeadlineMs, 0);
    }

    public static void Disconnect()
    {
        StopDeadline();
        s_tracker.TryTimeOut();
        HidTransport.Close();
        State = LinkState.Closed;
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
        SetState(LinkState.NoResponse);
    }

    static void OnReport(ReadOnlySpan<byte> report)
    {
        if (!HpReports.TryGetCommand(report, out byte command))
            return;

        if (s_tracker.TryComplete(command, Environment.TickCount64, out long elapsed))
        {
            StopDeadline();
            LatencyMs = elapsed;
        }

        // Matched by content: a late response, or one to NGENUITY's query, is just as valid.
        if (HpReports.TryParseBattery(report, out byte percent))
        {
            BatteryPercent = percent;
            SetState(LinkState.BatteryKnown);
        }
    }

    static void OnFailed(int error)
    {
        StopDeadline();
        s_tracker.TryTimeOut();
        LastError = error;
        SetState(LinkState.Failed);
    }

    static void StopDeadline() => KillTimer(s_window, DeadlineTimerId);

    static void SetState(LinkState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
