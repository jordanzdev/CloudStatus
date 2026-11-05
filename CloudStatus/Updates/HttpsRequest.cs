using System.Runtime.InteropServices;
using CloudStatus.Core;
using static CloudStatus.Native.WinHttp;

namespace CloudStatus.Updates;

/// <summary>
/// A synchronous HTTPS GET through WinHTTP (part of Windows: no extra dependencies, no extra
/// threads). Used only when the user asks for an update check, so blocking briefly is acceptable.
/// </summary>
internal unsafe struct HttpsRequest : IDisposable
{
    const int TimeoutMs = 15_000;
    const int ReadChunkBytes = 64 * 1024;

    nint _session;
    nint _connection;
    nint _request;

    public uint StatusCode { get; private set; }

    public readonly bool IsSuccess => StatusCode is >= 200 and < 300;

    public readonly bool IsRedirect => StatusCode is >= 300 and < 400;

    /// <summary>
    /// Sends a GET for <paramref name="path"/> on https://github.com and reads the status line. The
    /// host is fixed here: the app never connects anywhere else (spec 010). Dispose the result even
    /// when this returns false.
    /// </summary>
    public static bool TrySend(string path, bool followRedirects, out HttpsRequest request)
    {
        request = default;
        request._session = WinHttpOpen("CloudStatus/" + AppVersion.CurrentText, WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY, null, null, 0);
        if (request._session == 0)
            return false;

        WinHttpSetTimeouts(request._session, TimeoutMs, TimeoutMs, TimeoutMs, TimeoutMs);
        request._connection = WinHttpConnect(request._session, ReleaseInfo.Host, INTERNET_DEFAULT_HTTPS_PORT, 0);
        if (request._connection == 0)
            return false;

        request._request = WinHttpOpenRequest(request._connection, "GET", path, null, null, 0, WINHTTP_FLAG_SECURE);
        if (request._request == 0)
            return false;

        if (!followRedirects)
        {
            uint feature = WINHTTP_DISABLE_REDIRECTS;
            WinHttpSetOption(request._request, WINHTTP_OPTION_DISABLE_FEATURE, &feature, sizeof(uint));
        }

        if (!WinHttpSendRequest(request._request, null, 0, null, 0, 0, 0) || !WinHttpReceiveResponse(request._request, 0))
            return false;

        uint status = 0, size = sizeof(uint);
        if (!WinHttpQueryHeaders(request._request, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, null, &status, &size, null))
            return false;

        request.StatusCode = status;
        return true;
    }

    public readonly string? GetLocationHeader()
    {
        uint size = 0;
        WinHttpQueryHeaders(_request, WINHTTP_QUERY_LOCATION, null, null, &size, null);
        if (Marshal.GetLastPInvokeError() != ERROR_INSUFFICIENT_BUFFER || size == 0)
            return null;

        var buffer = new char[size / sizeof(char)];
        fixed (char* chars = buffer)
        {
            if (!WinHttpQueryHeaders(_request, WINHTTP_QUERY_LOCATION, null, chars, &size, null))
                return null;
            return new string(chars, 0, (int)(size / sizeof(char)));
        }
    }

    /// <summary>Reads the whole body. Fails if it is larger than <paramref name="maxBytes"/>.</summary>
    public readonly bool TryReadBody(int maxBytes, out byte[] body)
    {
        body = [];
        using var content = new MemoryStream();
        var chunk = new byte[ReadChunkBytes];
        fixed (byte* buffer = chunk)
        {
            while (true)
            {
                uint read = 0;
                if (!WinHttpReadData(_request, buffer, (uint)chunk.Length, &read))
                    return false;
                if (read == 0)
                    break;
                if (content.Length + read > maxBytes)
                    return false;
                content.Write(chunk, 0, (int)read);
            }
        }
        body = content.ToArray();
        return true;
    }

    public void Dispose()
    {
        CloseIfOpen(ref _request);
        CloseIfOpen(ref _connection);
        CloseIfOpen(ref _session);
    }

    static void CloseIfOpen(ref nint handle)
    {
        if (handle != 0)
            WinHttpCloseHandle(handle);
        handle = 0;
    }
}
