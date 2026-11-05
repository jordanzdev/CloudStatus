using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>
/// The application logo (embedded through ApplicationIcon) as a window's title bar and taskbar
/// icons, loaded at the exact size for the window's DPI. The caller owns and releases the icons.
/// </summary>
internal struct WindowIcons
{
    const nint ApplicationIconResourceId = 32512; // id the C# compiler gives the ApplicationIcon group

    nint _small;
    nint _big;

    /// <summary>The large logo, also shown by the close question (spec 013). Owned by this struct.</summary>
    public readonly nint Big => _big;

    public void Apply(nint window, uint dpi)
    {
        nint previousSmall = _small, previousBig = _big;
        _small = Load(GetSystemMetricsForDpi(SM_CXSMICON, dpi));
        _big = Load(GetSystemMetricsForDpi(SM_CXICON, dpi));
        SendMessageW(window, WM_SETICON, ICON_SMALL, _small);
        SendMessageW(window, WM_SETICON, ICON_BIG, _big);
        DestroyIfLoaded(previousSmall);
        DestroyIfLoaded(previousBig);
    }

    /// <summary>Call once the window is destroyed.</summary>
    public void Release()
    {
        DestroyIfLoaded(_small);
        DestroyIfLoaded(_big);
        _small = _big = 0;
    }

    static nint Load(int size) =>
        LoadImageW(AppInstance.Handle, ApplicationIconResourceId, IMAGE_ICON, size, size, LR_DEFAULTCOLOR);

    static void DestroyIfLoaded(nint icon)
    {
        if (icon != 0)
            DestroyIcon(icon);
    }
}
