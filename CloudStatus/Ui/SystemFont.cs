using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>The Windows message font, the one dialogs use. Callers set the height they need.</summary>
internal static unsafe class SystemFont
{
    const string FallbackFace = "Segoe UI";
    const int FaceNameCapacity = 32;
    const uint DefaultDpi = 96;

    public static LOGFONTW GetMessageFont()
    {
        NONCLIENTMETRICSW metrics = default;
        metrics.cbSize = (uint)sizeof(NONCLIENTMETRICSW);
        if (SystemParametersInfoForDpi(SPI_GETNONCLIENTMETRICS, metrics.cbSize, &metrics, 0, DefaultDpi))
            return metrics.lfMessageFont;

        LOGFONTW font = default;
        FallbackFace.AsSpan().CopyTo(new Span<char>(font.lfFaceName, FaceNameCapacity - 1));
        return font;
    }
}
