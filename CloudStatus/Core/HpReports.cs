namespace CloudStatus.Core;

/// <summary>Builds and reads HP protocol reports (docs/protocol.md, section 5). No allocations.</summary>
internal static class HpReports
{
    public const byte PowerCommand = 0x01;
    public const byte BatteryCommand = 0x02;
    public const byte ChargingCommand = 0x03;

    const byte ReportId = 0x06;
    const byte HeaderSecond = 0xFF;
    const byte HeaderThird = 0xBB;
    const int CommandIndex = 3;
    const int ValueIndex = 4;
    const int BatteryIndex = 7;
    const byte MaxPercent = 100;

    const byte PowerOn = 1;
    const byte PowerOnAlternative = 4;
    const byte NotCharging = 0;
    const byte Charging = 1;
    const byte Full = 2;

    /// <summary>Copies <paramref name="report"/> and zero pads the rest. Refuses a report that does not fit.</summary>
    public static bool TryPad(ReadOnlySpan<byte> report, Span<byte> destination)
    {
        if (report.Length > destination.Length)
            return false;
        report.CopyTo(destination);
        destination[report.Length..].Clear();
        return true;
    }

    public static bool TryGetCommand(ReadOnlySpan<byte> report, out byte command)
    {
        command = 0;
        if (report.Length <= CommandIndex || report[0] != ReportId || report[1] != HeaderSecond || report[2] != HeaderThird)
            return false;
        command = report[CommandIndex];
        return true;
    }

    public static bool TryParseBattery(ReadOnlySpan<byte> report, out byte percent)
    {
        percent = 0;
        if (report.Length <= BatteryIndex ||
            !TryGetCommand(report, out byte command) ||
            command != BatteryCommand ||
            report[BatteryIndex] > MaxPercent)
            return false;

        percent = report[BatteryIndex];
        return true;
    }

    public static bool TryParseCharging(ReadOnlySpan<byte> report, out ChargeState state)
    {
        state = ChargeState.NotCharging;
        if (!TryGetValue(report, ChargingCommand, out byte value))
            return false;

        state = value switch
        {
            NotCharging => ChargeState.NotCharging,
            Charging => ChargeState.Charging,
            Full => ChargeState.Full,
            _ => ChargeState.Error,
        };
        return true;
    }

    /// <summary>Anything but on counts as off: 3 is off, 2 is pairing, and neither can be used.</summary>
    public static bool TryParsePower(ReadOnlySpan<byte> report, out bool isOn)
    {
        isOn = false;
        if (!TryGetValue(report, PowerCommand, out byte value))
            return false;

        isOn = value is PowerOn or PowerOnAlternative;
        return true;
    }

    static bool TryGetValue(ReadOnlySpan<byte> report, byte expectedCommand, out byte value)
    {
        value = 0;
        if (report.Length <= ValueIndex || !TryGetCommand(report, out byte command) || command != expectedCommand)
            return false;
        value = report[ValueIndex];
        return true;
    }
}
