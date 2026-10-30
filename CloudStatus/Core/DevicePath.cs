using System.Globalization;

namespace CloudStatus.Core;

/// <summary>Reads Windows device interface paths such as \\?\HID#VID_03F0&amp;PID_018B&amp;MI_03#… No allocations.</summary>
internal static class DevicePath
{
    const string VendorMarker = "vid_";
    const string ProductMarker = "pid_";
    const int IdLength = 4;

    public static bool TryParseIds(ReadOnlySpan<char> path, out ushort vendorId, out ushort productId)
    {
        productId = 0;
        return TryParseId(path, VendorMarker, out vendorId) && TryParseId(path, ProductMarker, out productId);
    }

    /// <summary>Windows does not keep the case of a path between its APIs.</summary>
    public static bool IsSame(ReadOnlySpan<char> first, ReadOnlySpan<char> second) =>
        first.Equals(second, StringComparison.OrdinalIgnoreCase);

    static bool TryParseId(ReadOnlySpan<char> path, string marker, out ushort id)
    {
        id = 0;
        int start = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || start + marker.Length + IdLength > path.Length)
            return false;

        ReadOnlySpan<char> digits = path.Slice(start + marker.Length, IdLength);
        return ushort.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out id);
    }
}
