using System.Runtime.InteropServices;

namespace CloudStatus.Native;

internal static unsafe partial class WinHttp
{
    const string Lib = "winhttp.dll";

    public const uint WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY = 4;
    public const uint WINHTTP_FLAG_SECURE = 0x00800000;
    public const ushort INTERNET_DEFAULT_HTTPS_PORT = 443;
    public const uint WINHTTP_OPTION_DISABLE_FEATURE = 63;
    public const uint WINHTTP_DISABLE_REDIRECTS = 0x00000002;
    public const uint WINHTTP_QUERY_STATUS_CODE = 19;
    public const uint WINHTTP_QUERY_LOCATION = 33;
    public const uint WINHTTP_QUERY_FLAG_NUMBER = 0x20000000;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint WinHttpOpen(string userAgent, uint accessType, string? proxy, string? proxyBypass, uint flags);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpSetTimeouts(nint handle, int resolveMs, int connectMs, int sendMs, int receiveMs);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint WinHttpConnect(nint session, string server, ushort port, uint reserved);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint WinHttpOpenRequest(nint connection, string verb, string path, string? version,
        string? referrer, nint acceptTypes, uint flags);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpSetOption(nint handle, uint option, void* buffer, uint length);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpSendRequest(nint request, char* headers, uint headersLength, void* optional,
        uint optionalLength, uint totalLength, nint context);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpReceiveResponse(nint request, nint reserved);

    [LibraryImport(Lib, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpQueryHeaders(nint request, uint infoLevel, char* name, void* buffer,
        uint* bufferLength, uint* index);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpReadData(nint request, void* buffer, uint bytesToRead, uint* bytesRead);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinHttpCloseHandle(nint handle);
}

internal static unsafe partial class BCrypt
{
    public const nint BCRYPT_SHA256_ALG_HANDLE = 0x41; // pseudo-handle, Windows 10+
    public const int Sha256Length = 32;

    [LibraryImport("bcrypt.dll")]
    public static partial int BCryptHash(nint algorithm, byte* secret, uint secretLength,
        byte* input, uint inputLength, byte* output, uint outputLength);
}
