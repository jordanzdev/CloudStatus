using System.Runtime.InteropServices;
using CloudStatus.Core;
using CloudStatus.Native;

namespace CloudStatus.Device;

internal delegate void ReportHandler(ReadOnlySpan<byte> report);

/// <summary>
/// Overlapped I/O on the protocol interface, on the UI thread. One read is always pending, which
/// costs nothing until a report arrives. Nothing here blocks, except waiting for a cancellation on
/// close. Buffers and OVERLAPPED structures are native memory because the kernel writes to them.
/// </summary>
internal static unsafe class HidTransport
{
    const int MaxReportLength = 64;
    const int MaxReportsPerPump = 16; // the event stays signaled, so the rest arrive on the next wake-up
    const int MaxWaitHandles = 2;

    static nint s_device;
    static nint s_readEvent;
    static nint s_writeEvent;
    static void* s_memory;
    static OVERLAPPED* s_readOverlapped;
    static OVERLAPPED* s_writeOverlapped;
    static byte* s_readBuffer;
    static byte* s_writeBuffer;
    static nint* s_waitHandles;
    static int s_inputLength;
    static int s_outputLength;
    static bool s_isReadPending;
    static bool s_isWritePending;

    /// <summary>Raised for every report, whoever asked for it. The span is valid during the call only.</summary>
    public static event ReportHandler? ReportReceived;

    /// <summary>Raised with the Win32 error after an I/O failure has closed the transport.</summary>
    public static event Action<int>? Failed;

    public static bool IsOpen => s_device != 0;

    /// <summary>Handles for the message loop. The write event only counts while a write is pending.</summary>
    public static nint* WaitHandles => s_waitHandles;

    public static uint WaitCount => !IsOpen ? 0u : s_isWritePending ? 2u : 1u;

    public static bool TryOpen(in HidCandidate target)
    {
        Close();
        if (target.InputLength > MaxReportLength || target.OutputLength > MaxReportLength)
            return false;

        nint device = Kernel32.CreateFileW(target.Path, Kernel32.GENERIC_READ | Kernel32.GENERIC_WRITE,
            Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE, 0, Kernel32.OPEN_EXISTING, Kernel32.FILE_FLAG_OVERLAPPED, 0);
        if (device == Kernel32.INVALID_HANDLE_VALUE)
            return false;

        s_device = device;
        s_inputLength = target.InputLength;
        s_outputLength = target.OutputLength;
        s_readEvent = Kernel32.CreateEventW(0, true, false, 0);
        s_writeEvent = Kernel32.CreateEventW(0, true, false, 0);
        if (s_readEvent == 0 || s_writeEvent == 0)
        {
            Close();
            return false;
        }

        AllocateMemory();
        if (!TryArmRead())
        {
            Close();
            return false;
        }
        return true;
    }

    /// <summary>Sends one report. False if a write is pending, or if it does not fit the output report.</summary>
    public static bool TrySend(ReadOnlySpan<byte> report)
    {
        if (!IsOpen || s_isWritePending || !HpReports.TryPad(report, new Span<byte>(s_writeBuffer, s_outputLength)))
            return false;

        if (!Kernel32.WriteFile(s_device, s_writeBuffer, (uint)s_outputLength, null, s_writeOverlapped))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != Kernel32.ERROR_IO_PENDING)
            {
                Fail(error);
                return false;
            }
        }
        s_isWritePending = true;
        return true;
    }

    /// <summary>Handles whatever has completed, without blocking.</summary>
    public static void Pump()
    {
        if (IsOpen && s_isWritePending)
            CheckWrite();

        for (int i = 0; i < MaxReportsPerPump && IsOpen && s_isReadPending; i++)
        {
            if (!TryCompleteRead())
                break;
        }
    }

    public static void CancelWrite()
    {
        if (IsOpen && s_isWritePending)
            Kernel32.CancelIoEx(s_device, s_writeOverlapped);
    }

    /// <summary>Cancels I/O and waits for it, because the kernel owns the buffers until it finishes.</summary>
    public static void Close()
    {
        if (s_device == 0)
            return;

        if (s_isReadPending || s_isWritePending)
            Kernel32.CancelIoEx(s_device, null);

        uint transferred;
        if (s_isReadPending)
            Kernel32.GetOverlappedResult(s_device, s_readOverlapped, &transferred, true);
        if (s_isWritePending)
            Kernel32.GetOverlappedResult(s_device, s_writeOverlapped, &transferred, true);
        s_isReadPending = s_isWritePending = false;

        CloseIfOpen(ref s_readEvent);
        CloseIfOpen(ref s_writeEvent);
        CloseIfOpen(ref s_device);
        NativeMemory.Free(s_memory);
        s_memory = null;
        s_waitHandles = null;
    }

    static void AllocateMemory()
    {
        int overlappedSize = sizeof(OVERLAPPED);
        int handlesSize = MaxWaitHandles * sizeof(nint);
        var block = (byte*)NativeMemory.AllocZeroed((nuint)(2 * overlappedSize + handlesSize + s_inputLength + s_outputLength));

        s_memory = block;
        s_readOverlapped = (OVERLAPPED*)block;
        s_writeOverlapped = (OVERLAPPED*)(block + overlappedSize);
        s_waitHandles = (nint*)(block + 2 * overlappedSize);
        s_readBuffer = block + 2 * overlappedSize + handlesSize;
        s_writeBuffer = s_readBuffer + s_inputLength;

        s_readOverlapped->hEvent = s_readEvent;
        s_writeOverlapped->hEvent = s_writeEvent;
        s_waitHandles[0] = s_readEvent;
        s_waitHandles[1] = s_writeEvent;
    }

    static bool TryArmRead()
    {
        if (!Kernel32.ReadFile(s_device, s_readBuffer, (uint)s_inputLength, null, s_readOverlapped))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != Kernel32.ERROR_IO_PENDING)
                return false;
        }
        // Also when it finished at once: the event is signaled and Pump collects the report.
        s_isReadPending = true;
        return true;
    }

    /// <summary>False when no report has arrived yet, or the transport closed.</summary>
    static bool TryCompleteRead()
    {
        uint read;
        if (!Kernel32.GetOverlappedResult(s_device, s_readOverlapped, &read, false))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != Kernel32.ERROR_IO_INCOMPLETE)
                Fail(error);
            return false;
        }

        s_isReadPending = false;
        ReportReceived?.Invoke(new ReadOnlySpan<byte>(s_readBuffer, (int)read));
        if (!IsOpen)
            return false;

        if (TryArmRead())
            return true;
        Fail(Marshal.GetLastPInvokeError());
        return false;
    }

    static void CheckWrite()
    {
        uint written;
        if (Kernel32.GetOverlappedResult(s_device, s_writeOverlapped, &written, false))
        {
            s_isWritePending = false;
            Kernel32.ResetEvent(s_writeEvent);
            return;
        }

        int error = Marshal.GetLastPInvokeError();
        if (error == Kernel32.ERROR_IO_INCOMPLETE)
            return;
        if (error == Kernel32.ERROR_OPERATION_ABORTED)
        {
            s_isWritePending = false;
            Kernel32.ResetEvent(s_writeEvent);
            return;
        }
        Fail(error);
    }

    static void Fail(int error)
    {
        Close();
        Failed?.Invoke(error);
    }

    static void CloseIfOpen(ref nint handle)
    {
        if (handle != 0)
            Kernel32.CloseHandle(handle);
        handle = 0;
    }
}
