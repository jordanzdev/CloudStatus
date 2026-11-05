using CloudStatus.Native;

namespace CloudStatus.Ui;

internal static class Dpi
{
    public const uint Default = 96;

    /// <summary>Scales a value designed at 96 DPI.</summary>
    public static int Scale(int value, uint dpi) => Kernel32.MulDiv(value, (int)dpi, (int)Default);
}
