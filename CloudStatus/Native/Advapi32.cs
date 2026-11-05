using System.Runtime.InteropServices;

namespace CloudStatus.Native;

internal static unsafe partial class Advapi32
{
    const string Lib = "advapi32.dll";

    public const nint HKEY_CURRENT_USER = unchecked((int)0x80000001);
    public const uint KEY_READ = 0x20019;
    public const uint KEY_WRITE = 0x20006;
    public const uint REG_SZ = 1;
    public const uint REG_EXPAND_SZ = 2;
    public const uint REG_DWORD = 4;
    public const int ERROR_SUCCESS = 0;
    public const int ERROR_FILE_NOT_FOUND = 2;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegOpenKeyExW(nint key, string subKey, uint options, uint access, nint* result);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegCreateKeyExW(nint key, string subKey, uint reserved, string? className, uint options,
        uint access, nint securityAttributes, nint* result, uint* disposition);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegQueryValueExW(nint key, string? name, nint reserved, uint* type, byte* data, uint* dataSize);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegSetValueExW(nint key, string? name, uint reserved, uint type, byte* data, uint dataSize);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegDeleteValueW(nint key, string? name);

    [LibraryImport(Lib)]
    public static partial int RegCloseKey(nint key);
}
