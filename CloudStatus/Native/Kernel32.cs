using System.Runtime.InteropServices;

namespace CloudStatus.Native;

internal static partial class Kernel32
{
    const string Lib = "kernel32.dll";

    public const int ERROR_ALREADY_EXISTS = 183;
    public const ushort LANG_SPANISH = 0x0A;
    public const ushort PRIMARY_LANGUAGE_MASK = 0x3FF;

    public const uint NO_ACCESS = 0;
    public const uint FILE_SHARE_READ = 0x1;
    public const uint FILE_SHARE_WRITE = 0x2;
    public const uint OPEN_EXISTING = 3;
    public const nint INVALID_HANDLE_VALUE = -1;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint CreateMutexW(nint attributes, [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string name);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandleW(string? moduleName);

    [LibraryImport(Lib)]
    public static partial ushort GetUserDefaultUILanguage();
}
