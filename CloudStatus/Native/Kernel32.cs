using System.Runtime.InteropServices;

namespace CloudStatus.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct OVERLAPPED
{
    public nuint Internal;
    public nuint InternalHigh;
    public uint Offset;
    public uint OffsetHigh;
    public nint hEvent;
}

internal static unsafe partial class Kernel32
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
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
    public const int ERROR_OPERATION_ABORTED = 995;
    public const int ERROR_IO_INCOMPLETE = 996;
    public const int ERROR_IO_PENDING = 997;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint CreateMutexW(nint attributes, [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string name);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateFileW(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadFile(nint file, byte* buffer, uint length, uint* read, OVERLAPPED* overlapped);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteFile(nint file, byte* buffer, uint length, uint* written, OVERLAPPED* overlapped);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetOverlappedResult(nint file, OVERLAPPED* overlapped, uint* transferred, [MarshalAs(UnmanagedType.Bool)] bool wait);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CancelIoEx(nint file, OVERLAPPED* overlapped);

    [LibraryImport(Lib)]
    public static partial nint CreateEventW(nint attributes, [MarshalAs(UnmanagedType.Bool)] bool manualReset, [MarshalAs(UnmanagedType.Bool)] bool initialState, nint name);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ResetEvent(nint handle);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandleW(string? moduleName);

    [LibraryImport(Lib)]
    public static partial ushort GetUserDefaultUILanguage();
}
