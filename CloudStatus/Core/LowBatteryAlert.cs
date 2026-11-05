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
    public const int LowLimit = IconDescription.LowLimit;
    public const int CriticalLimit = 10;
    public const int RearmMargin = 5;

    readonly record struct Input(HeadsetPower Power, byte Percent, ChargeState Charge, bool IsChargeKnown);

    Input _last;
    bool _hasLast;
    bool _lowNotified;
    bool _criticalNotified;

    /// <summary>
    /// <paramref name="isChargeKnown"/> is false while the charging state of a headset that just
    /// turned on has not arrived yet: until then it is not known to be discharging.
    /// </summary>
    public BatteryNotice Evaluate(HeadsetPower power, byte percent, ChargeState charge, bool isChargeKnown)
    {
        var input = new Input(power, percent, charge, isChargeKnown);
        if (_hasLast && input == _last)
            return BatteryNotice.None;
        _last = input;
        _hasLast = true;

        if (power != HeadsetPower.On || !isChargeKnown)
            return BatteryNotice.None;

        if (charge is ChargeState.Charging or ChargeState.Full)
        {
            _lowNotified = _criticalNotified = false;
            return BatteryNotice.None;
        }

        if (percent >= LowLimit + RearmMargin)
            _lowNotified = false;
        if (percent >= CriticalLimit + RearmMargin)
            _criticalNotified = false;

        if (percent < CriticalLimit && !_criticalNotified)
        {
            _criticalNotified = true;
            if (percent < LowLimit)
                _lowNotified = true; // one notice per change: the critical one covers both
            return BatteryNotice.Critical;
        }

        if (percent < LowLimit && !_lowNotified)
        {
            _lowNotified = true;
            return BatteryNotice.Low;
        }

        return BatteryNotice.None;
    }
}
