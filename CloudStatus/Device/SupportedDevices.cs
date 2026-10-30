using CloudStatus.Core;

namespace CloudStatus.Device;

/// <summary>
/// The only list of supported dongles (docs/protocol.md, section 2). Adding a product ID is one
/// line. The vendor ID selects the protocol, so a new vendor also needs its packets.
/// </summary>
internal static class SupportedDevices
{
    const ushort HpVendorId = 0x03F0;
    const ushort HpUsagePage = 0xFF90;
    const ushort HpUsage = 0x0303;

    static readonly SupportedDevice[] s_all =
    [
        new(HpVendorId, 0x018B, HpUsagePage, HpUsage),
        new(HpVendorId, 0x0696, HpUsagePage, HpUsage),
    ];

    public static ReadOnlySpan<SupportedDevice> All => s_all;
}
