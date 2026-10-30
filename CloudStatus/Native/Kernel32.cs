using System.Runtime.InteropServices;

namespace CloudStatus.Native;

internal static partial class Kernel32
{
    const string Lib = "kernel32.dll";

    public const int ERROR_ALREADY_EXISTS = 183;
    public const ushort LANG_SPANISH = 0x0A;
    public const ushort PRIMARY_LANGUAGE_MASK = 0x3FF;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint CreateMutexW(nint attributes, [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string name);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandleW(string? moduleName);

    [LibraryImport(Lib)]
    public static partial ushort GetUserDefaultUILanguage();
}
