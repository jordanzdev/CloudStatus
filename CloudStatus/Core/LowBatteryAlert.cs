namespace CloudStatus.Core;

internal enum BatteryNotice
{
    None,
    Low,
    Critical,
}

/// <summary>
/// Decides when to warn about a low battery (spec 006): each level once, until charging or a
/// recovered battery rearms it. Pure and allocation-free; the caller shows the notice.
/// </summary>
internal struct LowBatteryAlert
{
    public const int CriticalLimit = 10;
    public const int RearmMargin = 5;

    readonly record struct Input(
        HeadsetPower Power, byte Percent, ChargeState Charge, bool IsChargeKnown, byte LowLimit, bool IsEnabled);

    Input _last;
    bool _hasLast;
    bool _lowNotified;
    bool _criticalNotified;

    /// <summary>
    /// <paramref name="isChargeKnown"/> is false while the charging state of a headset that just
    /// turned on has not arrived yet: until then it is not known to be discharging. With
    /// <paramref name="isEnabled"/> false a level that fires counts as notified but nothing is
    /// returned, so turning notices back on does not bring back a limit already crossed (spec 007).
    /// </summary>
    public BatteryNotice Evaluate(
        HeadsetPower power, byte percent, ChargeState charge, bool isChargeKnown, byte lowLimit, bool isEnabled)
    {
        var input = new Input(power, percent, charge, isChargeKnown, lowLimit, isEnabled);
        if (_hasLast && input == _last)
            return BatteryNotice.None;
        _last = input;
        _hasLast = true;

        BatteryNotice notice = Decide(power, percent, charge, isChargeKnown, lowLimit);
        return isEnabled ? notice : BatteryNotice.None;
    }

    BatteryNotice Decide(HeadsetPower power, byte percent, ChargeState charge, bool isChargeKnown, byte lowLimit)
    {
        if (power != HeadsetPower.On || !isChargeKnown)
            return BatteryNotice.None;

        if (charge is ChargeState.Charging or ChargeState.Full)
        {
            _lowNotified = _criticalNotified = false;
            return BatteryNotice.None;
        }

        if (percent >= lowLimit + RearmMargin)
            _lowNotified = false;
        if (percent >= CriticalLimit + RearmMargin)
            _criticalNotified = false;

        if (percent < CriticalLimit && !_criticalNotified)
        {
            _criticalNotified = true;
            if (percent < lowLimit)
                _lowNotified = true; // one notice per change: the critical one covers both
            return BatteryNotice.Critical;
        }

        if (percent < lowLimit && !_lowNotified)
        {
            _lowNotified = true;
            return BatteryNotice.Low;
        }

        return BatteryNotice.None;
    }
}
