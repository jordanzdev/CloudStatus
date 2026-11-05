namespace CloudStatus.Core;

internal enum PowerAction
{
    None,
    /// <summary>Cancel I/O, close the device and stop timers, as when the dongle is removed.</summary>
    Disconnect,
    /// <summary>Search again and open what is found as a new connection, even with the same path.</summary>
    Reconnect,
}

/// <summary>
/// What to do on each sleep message (spec 008), and whether device events count. Pure: messages go
/// in and actions come out.
/// </summary>
internal struct SuspendGate
{
    public bool IsSuspended { get; private set; }

    /// <summary>Device arrivals and removals are ignored until the resume message.</summary>
    public readonly bool AcceptsDeviceEvents => !IsSuspended;

    /// <summary>PBT_APMSUSPEND. A repeated one does nothing more.</summary>
    public PowerAction OnSuspend()
    {
        if (IsSuspended)
            return PowerAction.None;
        IsSuspended = true;
        return PowerAction.Disconnect;
    }

    /// <summary>
    /// PBT_APMRESUMEAUTOMATIC, sent on every resume. It reconnects even with no suspend before it,
    /// so a stale handle is never kept.
    /// </summary>
    public PowerAction OnResumeAutomatic()
    {
        IsSuspended = false;
        return PowerAction.Reconnect;
    }

    /// <summary>PBT_APMRESUMESUSPEND only follows PBT_APMRESUMEAUTOMATIC, which already reconnected.</summary>
    public readonly PowerAction OnResumeSuspend() => PowerAction.None;
}
