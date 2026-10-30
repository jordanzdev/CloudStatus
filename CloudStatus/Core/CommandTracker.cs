namespace CloudStatus.Core;

/// <summary>The one command in flight. Time comes from the caller, so every transition can be tested.</summary>
internal struct CommandTracker
{
    byte _command;
    long _startedAt;

    public bool IsInFlight { get; private set; }

    public bool TryStart(byte command, long now)
    {
        if (IsInFlight)
            return false;
        IsInFlight = true;
        _command = command;
        _startedAt = now;
        return true;
    }

    /// <summary>True when <paramref name="command"/> answers the one in flight.</summary>
    public bool TryComplete(byte command, long now, out long elapsed)
    {
        elapsed = 0;
        if (!IsInFlight || command != _command)
            return false;
        IsInFlight = false;
        elapsed = now - _startedAt;
        return true;
    }

    /// <summary>Ends the command with no response. True if one was in flight.</summary>
    public bool TryTimeOut()
    {
        bool wasInFlight = IsInFlight;
        IsInFlight = false;
        return wasInFlight;
    }
}
