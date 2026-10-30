namespace CloudStatus.Core;

/// <summary>Builds and reads HP protocol reports (docs/protocol.md, section 5). No allocations.</summary>
internal static class HpReports
{
    public const byte BatteryCommand = 0x02;

    const byte ReportId = 0x06;
    const byte HeaderSecond = 0xFF;
    const byte HeaderThird = 0xBB;
    const int CommandIndex = 3;
    const int BatteryIndex = 7;
    const byte MaxPercent = 100;

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
}
