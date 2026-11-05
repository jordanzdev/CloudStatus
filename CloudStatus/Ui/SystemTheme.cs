using System.Runtime.InteropServices;
using CloudStatus.Core;
using CloudStatus.Native;
using CloudStatus.Persistence;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>
/// The current Windows theme (spec 013). Read at start-up and again only when Windows announces a
/// change: nothing polls. Also sets the theme of the tray menu.
/// </summary>
internal static unsafe class SystemTheme
{
    const string ImmersiveColorSet = "ImmersiveColorSet";

    public static Theme Current { get; private set; } = Theme.Default;

    /// <summary>The Windows build number, for the features that depend on it.</summary>
    public static int Build { get; } = Environment.OSVersion.Version.Build;

    public static void Load()
    {
        Current = Read();
        // Menus start in the system's light mode, so only a dark theme needs the call at start-up.
        if (Current.IsAppDark && !Current.IsHighContrast)
            ApplyToMenus();
    }

    /// <summary>Whether a broadcast may have changed the theme (WM_SETTINGCHANGE or WM_SYSCOLORCHANGE).</summary>
    public static bool MayHaveChanged(uint message, nint wParam, nint lParam) => message switch
    {
        WM_SYSCOLORCHANGE => true,
        WM_SETTINGCHANGE => (uint)wParam == SPI_SETHIGHCONTRAST || IsImmersiveColorSet(lParam),
        _ => false,
    };

    /// <summary>Reads the theme again. Returns true if it changed.</summary>
    public static bool Refresh()
    {
        Theme next = Read();
        if (next == Current)
            return false;
        Current = next;
        ApplyToMenus();
        return true;
    }

    static Theme Read()
    {
        WindowsTheme.Read(out uint? apps, out uint? taskbar);
        return Theme.Decide(apps, taskbar, IsHighContrastOn());
    }

    static bool IsHighContrastOn()
    {
        var highContrast = new HIGHCONTRASTW { cbSize = (uint)sizeof(HIGHCONTRASTW) };
        return SystemParametersInfoW(SPI_GETHIGHCONTRAST, highContrast.cbSize, &highContrast, 0) &&
            (highContrast.dwFlags & HCF_HIGHCONTRASTON) != 0;
    }

    /// <summary>Dark or light tray menu (REQ-013-22). High contrast leaves menus to Windows.</summary>
    static void ApplyToMenus()
    {
        if (!MenuDarkMode.IsSupported(Build))
            return;
        int mode = Current.IsHighContrast ? UxTheme.AppModeDefault
            : Current.IsAppDark ? UxTheme.AppModeForceDark
            : UxTheme.AppModeForceLight;
        UxTheme.TrySetMenuMode(mode); // on failure the menu simply keeps its look
    }

    static bool IsImmersiveColorSet(nint lParam) =>
        lParam != 0 && MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)lParam).SequenceEqual(ImmersiveColorSet);
}
