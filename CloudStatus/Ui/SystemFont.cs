using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>The Windows message font, the one dialogs use, for a given DPI.</summary>
internal static unsafe class SystemFont
{
    const string FallbackFace = "Segoe UI";
    const int FallbackSize = 12;
    const int FaceNameCapacity = 32;

    public static LOGFONTW GetMessageFont(uint dpi = Dpi.Default)
    {
        NONCLIENTMETRICSW metrics = default;
        metrics.cbSize = (uint)sizeof(NONCLIENTMETRICSW);
        if (SystemParametersInfoForDpi(SPI_GETNONCLIENTMETRICS, metrics.cbSize, &metrics, 0, dpi))
            return metrics.lfMessageFont;

        LOGFONTW font = default;
        font.lfHeight = -Dpi.Scale(FallbackSize, dpi);
        FallbackFace.AsSpan().CopyTo(new Span<char>(font.lfFaceName, FaceNameCapacity - 1));
        return font;
    }

    /// <summary>A font the caller must delete, or 0.</summary>
    public static nint CreateMessageFont(uint dpi)
    {
        LOGFONTW font = GetMessageFont(dpi);
        return Gdi32.CreateFontIndirectW(&font);
    }
}
