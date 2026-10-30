using System.Runtime.InteropServices;
using CloudStatus.Core;
using CloudStatus.Native;

namespace CloudStatus.Device;

/// <summary>
/// Finds the protocol interface of a connected dongle. Interfaces are opened with access 0, which
/// only reads the descriptor Windows has cached: nothing reaches the device. Allocates, so it runs
/// only at start-up and when a supported dongle arrives or leaves.
/// </summary>
internal static unsafe class HidDiscovery
{
    const int MaxListAttempts = 3;

    public static bool TryFind(out HidCandidate selected)
    {
        var candidates = new List<HidCandidate>();
        char[]? list = ReadInterfaceList();
        if (list != null)
        {
            // A sequence of null-terminated paths, ended by an empty one.
            ReadOnlySpan<char> remaining = list;
            while (remaining.Length > 0 && remaining[0] != '\0')
            {
                int length = remaining.IndexOf('\0');
                if (length < 0)
                    break;
                AddIfSupported(remaining[..length], candidates);
                remaining = remaining[(length + 1)..];
            }
        }
        return InterfaceSelector.TrySelect(CollectionsMarshal.AsSpan(candidates), SupportedDevices.All, out selected);
    }

    static void AddIfSupported(ReadOnlySpan<char> path, List<HidCandidate> candidates)
    {
        if (!DevicePath.TryParseIds(path, out ushort vendorId, out ushort productId) ||
            !InterfaceSelector.IsSupported(vendorId, productId, SupportedDevices.All))
            return;

        string pathText = path.ToString();
        if (TryReadCaps(pathText, out HIDP_CAPS caps))
        {
            candidates.Add(new HidCandidate(pathText, vendorId, productId, caps.UsagePage, caps.Usage,
                caps.InputReportByteLength, caps.OutputReportByteLength));
        }
    }

    /// <summary>The list can grow between the two calls if a device arrives, so it is retried.</summary>
    static char[]? ReadInterfaceList()
    {
        Guid hidClass = Hid.GUID_DEVINTERFACE_HID;
        for (int attempt = 0; attempt < MaxListAttempts; attempt++)
        {
            uint length;
            if (CfgMgr32.CM_Get_Device_Interface_List_SizeW(&length, &hidClass, null, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CfgMgr32.CR_SUCCESS)
                return null;

            var buffer = new char[length];
            fixed (char* chars = buffer)
            {
                uint result = CfgMgr32.CM_Get_Device_Interface_ListW(&hidClass, null, chars, length, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT);
                if (result == CfgMgr32.CR_SUCCESS)
                    return buffer;
                if (result != CfgMgr32.CR_BUFFER_SMALL)
                    return null;
            }
        }
        return null;
    }

    static bool TryReadCaps(string path, out HIDP_CAPS caps)
    {
        caps = default;
        nint device = Kernel32.CreateFileW(path, Kernel32.NO_ACCESS, Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE,
            0, Kernel32.OPEN_EXISTING, 0, 0);
        if (device == Kernel32.INVALID_HANDLE_VALUE)
            return false;

        try
        {
            nint preparsedData;
            if (!Hid.HidD_GetPreparsedData(device, &preparsedData))
                return false;

            try
            {
                HIDP_CAPS read;
                if (Hid.HidP_GetCaps(preparsedData, &read) != Hid.HIDP_STATUS_SUCCESS)
                    return false;
                caps = read;
                return true;
            }
            finally
            {
                Hid.HidD_FreePreparsedData(preparsedData);
            }
        }
        finally
        {
            Kernel32.CloseHandle(device);
        }
    }
}
