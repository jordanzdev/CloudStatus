using System.Runtime.InteropServices;
using CloudStatus.Core;
using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Device;

/// <summary>
/// Knows whether a supported dongle is connected. Windows reports arrivals and removals through
/// WM_DEVICECHANGE, so there are no timers and nothing runs between events.
/// </summary>
internal static unsafe class DongleWatcher
{
    static nint s_notification;
    static HidCandidate s_selected;
    static bool s_isConnected;

    /// <summary>Raised when <see cref="IsConnected"/> changes.</summary>
    public static event Action? Changed;

    public static bool IsConnected => s_isConnected;

    /// <summary>Registers first and searches second, so a dongle plugged in between them is not missed.</summary>
    public static void Start(nint window)
    {
        var filter = new DEV_BROADCAST_DEVICEINTERFACE_W
        {
            dbcc_size = (uint)sizeof(DEV_BROADCAST_DEVICEINTERFACE_W),
            dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
            dbcc_classguid = Hid.GUID_DEVINTERFACE_HID,
        };
        s_notification = RegisterDeviceNotificationW(window, &filter, DEVICE_NOTIFY_WINDOW_HANDLE);
        Search();
    }

    public static void Stop()
    {
        if (s_notification != 0)
            UnregisterDeviceNotification(s_notification);
        s_notification = 0;
    }

    /// <summary>Handles WM_DEVICECHANGE. Devices that are not ours return here without allocating.</summary>
    public static void OnDeviceChange(nint change, nint data)
    {
        if ((change != DBT_DEVICEARRIVAL && change != DBT_DEVICEREMOVECOMPLETE) || data == 0)
            return;

        var header = (DEV_BROADCAST_DEVICEINTERFACE_W*)data;
        if (header->dbcc_devicetype != DBT_DEVTYP_DEVICEINTERFACE)
            return;

        ReadOnlySpan<char> path = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(header->dbcc_name);
        if (change == DBT_DEVICEARRIVAL)
            OnArrival(path);
        else
            OnRemoval(path);
    }

    /// <summary>A dongle sends one arrival per HID collection, and the wanted one may not be the first.</summary>
    static void OnArrival(ReadOnlySpan<char> path)
    {
        if (!s_isConnected &&
            DevicePath.TryParseIds(path, out ushort vendorId, out ushort productId) &&
            InterfaceSelector.IsSupported(vendorId, productId, SupportedDevices.All))
            Search();
    }

    static void OnRemoval(ReadOnlySpan<char> path)
    {
        if (s_isConnected && DevicePath.IsSame(path, s_selected.Path))
            Search(); // finds nothing, or another dongle if there is one
    }

    static void Search()
    {
        bool wasConnected = s_isConnected;
        s_isConnected = HidDiscovery.TryFind(out s_selected);
        if (s_isConnected != wasConnected)
            Changed?.Invoke();
    }
}
