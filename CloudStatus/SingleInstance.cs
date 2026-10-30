using System.Runtime.InteropServices;
using CloudStatus.Native;

namespace CloudStatus;

/// <summary>Named mutex that keeps a single running instance. Held until disposed.</summary>
internal readonly struct SingleInstance : IDisposable
{
    readonly nint _mutex;

    SingleInstance(nint mutex) => _mutex = mutex;

    public bool IsAcquired => _mutex != 0;

    public static SingleInstance TryAcquire(string name)
    {
        nint mutex = Kernel32.CreateMutexW(0, false, name);
        if (mutex != 0 && Marshal.GetLastPInvokeError() == Kernel32.ERROR_ALREADY_EXISTS)
        {
            Kernel32.CloseHandle(mutex);
            mutex = 0;
        }
        return new SingleInstance(mutex);
    }

    public void Dispose()
    {
        if (IsAcquired)
            Kernel32.CloseHandle(_mutex);
    }
}
