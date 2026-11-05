using System.Runtime.InteropServices;

namespace CloudStatus.Native;

/// <summary>
/// Visual styles. <see cref="SetWindowTheme"/> is documented. The menu functions are not: Windows
/// only exports them by ordinal, so they are looked up here and nowhere else (D21, enforced by
/// tools/Test-ForbiddenApis.ps1). Callers must check <c>MenuDarkMode.IsSupported</c> first, because
/// an ordinal may name another function on builds before 18362.
/// </summary>
internal static unsafe partial class UxTheme
{
    const string Lib = "uxtheme.dll";

    const nint SetPreferredAppModeOrdinal = 135;
    const nint FlushMenuThemesOrdinal = 136;
    const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

    /// <summary>Values of <c>PreferredAppMode</c>.</summary>
    public const int AppModeDefault = 0;
    public const int AppModeForceDark = 2;
    public const int AppModeForceLight = 3;

    /// <summary>Common Controls draw dark scroll bars, edit menus and combo boxes with this class name.</summary>
    public const string DarkControlsClass = "DarkMode_CFD";

    static nint s_module;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SetWindowTheme(nint hwnd, string? subAppName, string? subIdList);

    /// <summary>
    /// Sets the mode of the menus this process shows and refreshes their cached theme. Returns false
    /// if either export is missing, in which case nothing was changed.
    /// </summary>
    public static bool TrySetMenuMode(int mode)
    {
        if (s_module == 0)
            s_module = LoadLibraryExW(Lib, 0, LOAD_LIBRARY_SEARCH_SYSTEM32); // kept loaded, like any import
        if (s_module == 0)
            return false;

        var setPreferredAppMode = (delegate* unmanaged<int, int>)GetProcAddress(s_module, SetPreferredAppModeOrdinal);
        var flushMenuThemes = (delegate* unmanaged<void>)GetProcAddress(s_module, FlushMenuThemesOrdinal);
        if (setPreferredAppMode == null || flushMenuThemes == null)
            return false;

        setPreferredAppMode(mode);
        flushMenuThemes();
        return true;
    }

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint LoadLibraryExW(string fileName, nint file, uint flags);

    /// <summary>Takes an ordinal in the low word, as MAKEINTRESOURCE does.</summary>
    [LibraryImport("kernel32.dll")]
    private static partial nint GetProcAddress(nint module, nint ordinal);
}
