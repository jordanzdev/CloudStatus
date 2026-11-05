using CloudStatus.Native;

namespace CloudStatus.Persistence;

/// <summary>Owned handle to a key under HKEY_CURRENT_USER. Disposing it closes the key.</summary>
internal readonly unsafe struct RegistryKeyHandle : IDisposable
{
    const uint MaxStringBytes = 64 * 1024;

    readonly nint _handle;

    RegistryKeyHandle(nint handle) => _handle = handle;

    public bool IsOpen => _handle != 0;

    public static RegistryKeyHandle OpenForRead(string path)
    {
        nint handle;
        int result = Advapi32.RegOpenKeyExW(Advapi32.HKEY_CURRENT_USER, path, 0, Advapi32.KEY_READ, &handle);
        return new RegistryKeyHandle(result == Advapi32.ERROR_SUCCESS ? handle : 0);
    }

    public static RegistryKeyHandle OpenForWrite(string path)
    {
        nint handle;
        uint disposition;
        int result = Advapi32.RegCreateKeyExW(Advapi32.HKEY_CURRENT_USER, path, 0, null, 0,
            Advapi32.KEY_READ | Advapi32.KEY_WRITE, 0, &handle, &disposition);
        return new RegistryKeyHandle(result == Advapi32.ERROR_SUCCESS ? handle : 0);
    }

    public bool TryGetDword(string name, out uint value)
    {
        uint data = 0, type = 0, size = sizeof(uint);
        bool found = IsOpen &&
            Advapi32.RegQueryValueExW(_handle, name, 0, &type, (byte*)&data, &size) == Advapi32.ERROR_SUCCESS &&
            type == Advapi32.REG_DWORD &&
            size == sizeof(uint);
        value = found ? data : 0;
        return found;
    }

    public string? GetString(string name)
    {
        if (!IsOpen)
            return null;

        uint type = 0, size = 0;
        if (Advapi32.RegQueryValueExW(_handle, name, 0, &type, null, &size) != Advapi32.ERROR_SUCCESS)
            return null;
        if (type != Advapi32.REG_SZ && type != Advapi32.REG_EXPAND_SZ)
            return null;
        if (size == 0)
            return "";
        if (size > MaxStringBytes)
            return null;

        var buffer = new char[size / sizeof(char) + 1];
        fixed (char* chars = buffer)
        {
            if (Advapi32.RegQueryValueExW(_handle, name, 0, &type, (byte*)chars, &size) != Advapi32.ERROR_SUCCESS)
                return null;
            return new string(chars, 0, LengthWithoutTrailingNulls(chars, (int)(size / sizeof(char))));
        }
    }

    public bool SetDword(string name, uint value) =>
        IsOpen && Advapi32.RegSetValueExW(_handle, name, 0, Advapi32.REG_DWORD, (byte*)&value, sizeof(uint)) == Advapi32.ERROR_SUCCESS;

    public bool SetString(string name, string value)
    {
        if (!IsOpen)
            return false;
        fixed (char* chars = value)
        {
            uint sizeIncludingNull = (uint)((value.Length + 1) * sizeof(char));
            return Advapi32.RegSetValueExW(_handle, name, 0, Advapi32.REG_SZ, (byte*)chars, sizeIncludingNull) == Advapi32.ERROR_SUCCESS;
        }
    }

    public bool DeleteValue(string name)
    {
        if (!IsOpen)
            return false;
        int result = Advapi32.RegDeleteValueW(_handle, name);
        return result == Advapi32.ERROR_SUCCESS || result == Advapi32.ERROR_FILE_NOT_FOUND;
    }

    public void Dispose()
    {
        if (IsOpen)
            Advapi32.RegCloseKey(_handle);
    }

    static int LengthWithoutTrailingNulls(char* chars, int length)
    {
        while (length > 0 && chars[length - 1] == '\0')
            length--;
        return length;
    }
}
