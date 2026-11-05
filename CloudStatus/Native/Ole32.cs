using System.Runtime.InteropServices;

namespace CloudStatus.Native;

internal static unsafe partial class Ole32
{
    const string Lib = "ole32.dll";

    public const uint COINIT_APARTMENTTHREADED = 0x2;
    public const uint CLSCTX_INPROC_SERVER = 0x1;
    public const uint STGM_READ = 0x0;

    public static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
    public static readonly Guid IID_IShellLinkW = new("000214F9-0000-0000-C000-000000000046");
    public static readonly Guid IID_IPersistFile = new("0000010B-0000-0000-C000-000000000046");

    [LibraryImport(Lib)]
    public static partial int CoInitializeEx(nint reserved, uint coInit);

    [LibraryImport(Lib)]
    public static partial void CoUninitialize();

    [LibraryImport(Lib)]
    public static partial int CoCreateInstance(Guid* clsid, nint outer, uint context, Guid* iid, nint* instance);
}

/// <summary>
/// Minimal IShellLinkW + IPersistFile, called through their vtables (no COM interop layer,
/// so it stays NativeAOT-friendly). Slot numbers follow the order of shobjidl.h / objidl.h.
/// </summary>
internal unsafe struct ShellLinkObject : IDisposable
{
    const int QueryInterfaceSlot = 0;
    const int ReleaseSlot = 2;
    const int GetPathSlot = 3;              // IShellLinkW
    const int SetWorkingDirectorySlot = 9;
    const int SetPathSlot = 20;
    const int LoadSlot = 5;                 // IPersistFile
    const int SaveSlot = 6;
    const int MaxPath = 260;

    nint _link;
    nint _file;

    /// <summary>Creates the shell link object. Dispose the result even when this returns false.</summary>
    public static bool TryCreate(out ShellLinkObject shellLink)
    {
        shellLink = default;
        Guid clsid = Ole32.CLSID_ShellLink, linkIid = Ole32.IID_IShellLinkW, fileIid = Ole32.IID_IPersistFile;
        nint link, file;
        if (Ole32.CoCreateInstance(&clsid, 0, Ole32.CLSCTX_INPROC_SERVER, &linkIid, &link) < 0)
            return false;
        shellLink._link = link;

        var queryInterface = (delegate* unmanaged<nint, Guid*, nint*, int>)Slot(link, QueryInterfaceSlot);
        if (queryInterface(link, &fileIid, &file) < 0)
            return false;
        shellLink._file = file;
        return true;
    }

    public readonly bool SetTarget(string path, string workingDirectory)
    {
        var setPath = (delegate* unmanaged<nint, char*, int>)Slot(_link, SetPathSlot);
        var setWorkingDirectory = (delegate* unmanaged<nint, char*, int>)Slot(_link, SetWorkingDirectorySlot);
        fixed (char* target = path, directory = workingDirectory)
            return setPath(_link, target) >= 0 && setWorkingDirectory(_link, directory) >= 0;
    }

    public readonly string? GetTarget()
    {
        var getPath = (delegate* unmanaged<nint, char*, int, nint, uint, int>)Slot(_link, GetPathSlot);
        char* buffer = stackalloc char[MaxPath];
        return getPath(_link, buffer, MaxPath, 0, 0) >= 0 ? new string(buffer) : null;
    }

    public readonly bool Save(string path)
    {
        var save = (delegate* unmanaged<nint, char*, int, int>)Slot(_file, SaveSlot);
        fixed (char* file = path)
            return save(_file, file, 1) >= 0;
    }

    public readonly bool Load(string path)
    {
        var load = (delegate* unmanaged<nint, char*, uint, int>)Slot(_file, LoadSlot);
        fixed (char* file = path)
            return load(_file, file, Ole32.STGM_READ) >= 0;
    }

    public void Dispose()
    {
        Release(ref _file);
        Release(ref _link);
    }

    static void Release(ref nint instance)
    {
        if (instance != 0)
            ((delegate* unmanaged<nint, uint>)Slot(instance, ReleaseSlot))(instance);
        instance = 0;
    }

    static void* Slot(nint instance, int slot) => (*(void***)instance)[slot];
}
