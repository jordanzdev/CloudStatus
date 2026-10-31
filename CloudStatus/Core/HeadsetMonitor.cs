namespace CloudStatus.Core;

internal enum HeadsetPower
{
    NoDongle,
    Checking,
    On,
    Off,
}

internal enum ChargeState
{
    NotCharging,
    Charging,
    Full,
    Error,
}

[Flags]
internal enum MonitorAction
{
    None = 0,
    QueryBattery = 1,
    QueryCharging = 2,
    StartPolling = 4,
    StopPolling = 8,
}

/// <summary>
/// What is known about the headset, and what to do next. It calls nothing: events go in and actions
/// come out, so every transition can be tested. Charging and power arrive as push events, so only
/// the battery needs a query (docs/protocol.md, Push events).
/// </summary>
internal struct HeadsetMonitor
{
    const int MaxFailedPolls = 2;

    enum Expected
    {
        Nothing,
        Battery,
        Charging,
    }

    Expected _expected;
    int _failedPolls;

    public HeadsetPower Power { get; private set; }

    public byte BatteryPercent { get; private set; }

    public ChargeState Charge { get; private set; }

    public MonitorAction OnDongleConnected() => StartChecking();

    public MonitorAction OnDongleLost()
    {
        this = default;
        return MonitorAction.StopPolling;
    }

    public MonitorAction OnPollDue()
    {
        if (Power != HeadsetPower.On)
            return MonitorAction.None;
        _expected = Expected.Battery;
        return MonitorAction.QueryBattery;
    }

    /// <summary>Also runs for responses to another program's query: they are just as valid.</summary>
    public MonitorAction OnBattery(byte percent)
    {
        switch (Power)
        {
            case HeadsetPower.Checking when percent == 0:
                return GoOff();

            case HeadsetPower.Checking:
                BatteryPercent = percent;
                Power = HeadsetPower.On;
                _failedPolls = 0;
                _expected = Expected.Charging;
                return MonitorAction.QueryCharging | MonitorAction.StartPolling;

            case HeadsetPower.On:
                BatteryPercent = percent;
                _failedPolls = 0;
                if (_expected == Expected.Battery)
                    _expected = Expected.Nothing;
                return MonitorAction.None;

            default:
                return MonitorAction.None;
        }
    }

    public MonitorAction OnCharging(ChargeState state)
    {
        Charge = state;
        if (_expected == Expected.Charging)
            _expected = Expected.Nothing;
        return MonitorAction.None;
    }

    public MonitorAction OnPower(bool isOn)
    {
        if (Power == HeadsetPower.NoDongle)
            return MonitorAction.None;
        if (!isOn)
            return GoOff();
        return Power == HeadsetPower.Off ? StartChecking() : MonitorAction.None;
    }

    public MonitorAction OnTimeout()
    {
        Expected expected = _expected;
        _expected = Expected.Nothing;

        if (Power == HeadsetPower.Checking)
            return GoOff();
        if (Power != HeadsetPower.On || expected != Expected.Battery)
            return MonitorAction.None;

        // Power off is pushed, so this only catches a push that was missed.
        return ++_failedPolls >= MaxFailedPolls ? GoOff() : MonitorAction.None;
    }

    MonitorAction StartChecking()
    {
        Power = HeadsetPower.Checking;
        _expected = Expected.Battery;
        _failedPolls = 0;
        return MonitorAction.QueryBattery;
    }

    MonitorAction GoOff()
    {
        Power = HeadsetPower.Off;
        _expected = Expected.Nothing;
        _failedPolls = 0;
        return MonitorAction.StopPolling;
    }
}
