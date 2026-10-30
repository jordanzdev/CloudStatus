namespace CloudStatus.Device;

/// <summary>
/// Every report CloudStatus can send (ARCHITECTURE.md, D10). Each one must be allowed by section 6
/// of docs/protocol.md, and a unit test asserts this exact set. Never add command 0x01: on this
/// headset it resets the state and unmutes the microphone.
/// </summary>
internal static class Commands
{
    /// <summary>HP battery query, command 0x02 (docs/protocol.md, section 6).</summary>
    public static readonly byte[] QueryBattery = [0x06, 0xFF, 0xBB, 0x02, 0x00];

    /// <summary>HP charging query, command 0x03 (docs/protocol.md, section 6).</summary>
    public static readonly byte[] QueryCharging = [0x06, 0xFF, 0xBB, 0x03, 0x00];

    public static readonly byte[][] All = [QueryBattery, QueryCharging];
}
