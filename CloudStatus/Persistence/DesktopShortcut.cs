using CloudStatus.Native;

namespace CloudStatus.Persistence;

/// <summary>
/// The optional desktop shortcut, created on request from the installer's last page
/// (CloudStatus.exe --create-desktop-shortcut) and removed on uninstall.
/// </summary>
internal static class DesktopShortcut
{
    const string FileName = "CloudStatus.lnk";

    static string LinkPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), FileName);

    /// <summary>Creates (or refreshes) a desktop shortcut to this executable.</summary>
    public static bool Create()
    {
        string? executable = Environment.ProcessPath;
        if (executable == null)
            return false;

        using var com = new ComApartment();
        if (!ShellLinkObject.TryCreate(out ShellLinkObject link))
        {
            link.Dispose();
            return false;
        }
        using (link)
            return link.SetTarget(executable, Path.GetDirectoryName(executable)!) && link.Save(LinkPath);
    }

    /// <summary>Deletes the desktop shortcut if it opens this executable (used when uninstalling).</summary>
    public static void RemoveIfTargetsCurrentExecutable()
    {
        string path = LinkPath;
        if (!File.Exists(path) || Environment.ProcessPath is not { } executable)
            return;

        using var com = new ComApartment();
        if (!ShellLinkObject.TryCreate(out ShellLinkObject link))
        {
            link.Dispose();
            return;
        }

        bool targetsThisExecutable;
        using (link)
            targetsThisExecutable = link.Load(path) &&
                string.Equals(link.GetTarget(), executable, StringComparison.OrdinalIgnoreCase);

        if (targetsThisExecutable)
            File.Delete(path);
    }

    /// <summary>COM for the calling thread, for the lifetime of the value.</summary>
    readonly struct ComApartment : IDisposable
    {
        readonly bool _initialized;

        public ComApartment() => _initialized = Ole32.CoInitializeEx(0, Ole32.COINIT_APARTMENTTHREADED) >= 0;

        public void Dispose()
        {
            if (_initialized)
                Ole32.CoUninitialize();
        }
    }
}
