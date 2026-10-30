using System.Runtime.InteropServices;

namespace CloudStatus.Native;

internal static unsafe partial class CfgMgr32
{
    const string Lib = "cfgmgr32.dll";

    public const uint CR_SUCCESS = 0x00;
    public const uint CR_BUFFER_SMALL = 0x1A;
    public const uint CM_GET_DEVICE_INTERFACE_LIST_PRESENT = 0;

    [LibraryImport(Lib)]
    public static partial uint CM_Get_Device_Interface_List_SizeW(uint* length, Guid* interfaceClass, char* deviceId, uint flags);

    [LibraryImport(Lib)]
    public static partial uint CM_Get_Device_Interface_ListW(Guid* interfaceClass, char* deviceId, char* buffer, uint length, uint flags);
}
