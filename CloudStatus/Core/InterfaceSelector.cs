namespace CloudStatus.Core;

/// <summary>A dongle CloudStatus supports, and the HID interface that speaks its protocol.</summary>
internal readonly record struct SupportedDevice(ushort VendorId, ushort ProductId, ushort UsagePage, ushort Usage);

/// <summary>What the HID descriptor of one interface says.</summary>
internal readonly record struct HidCandidate(string Path, ushort VendorId, ushort ProductId, ushort UsagePage,
    ushort Usage, ushort InputLength, ushort OutputLength);

/// <summary>Chooses the protocol interface from descriptor values, never by writing to a device.</summary>
internal static class InterfaceSelector
{
    public static bool IsSupported(ushort vendorId, ushort productId, ReadOnlySpan<SupportedDevice> table)
    {
        foreach (SupportedDevice device in table)
        {
            if (device.VendorId == vendorId && device.ProductId == productId)
                return true;
        }
        return false;
    }

    /// <summary>Takes the first device of the table that matches and, within it, the lowest path, so the choice is stable.</summary>
    public static bool TrySelect(ReadOnlySpan<HidCandidate> candidates, ReadOnlySpan<SupportedDevice> table, out HidCandidate selected)
    {
        selected = default;
        foreach (SupportedDevice device in table)
        {
            bool found = false;
            foreach (HidCandidate candidate in candidates)
            {
                if (Matches(candidate, device) && (!found || string.CompareOrdinal(candidate.Path, selected.Path) < 0))
                {
                    selected = candidate;
                    found = true;
                }
            }
            if (found)
                return true;
        }
        return false;
    }

    static bool Matches(in HidCandidate candidate, in SupportedDevice device) =>
        candidate.VendorId == device.VendorId &&
        candidate.ProductId == device.ProductId &&
        candidate.UsagePage == device.UsagePage &&
        candidate.Usage == device.Usage &&
        candidate.InputLength > 0 &&
        candidate.OutputLength > 0;
}
