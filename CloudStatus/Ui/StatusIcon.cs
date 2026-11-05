using CloudStatus.Core;
using CloudStatus.Native;
using CloudStatus.Ui.Drawing;
using static CloudStatus.Native.Gdi32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>
/// Draws the tray icon in memory (specs 005 and 013): a plate, a plain number, an outline or the
/// headset glyph. Shapes are computed pixel by pixel in straight alpha. GDI text does not write
/// alpha, so the number is drawn on a separate mask and its brightness becomes coverage.
/// </summary>
internal static unsafe class StatusIcon
{
    const float CornerShare = 0.2f;
    const float HollowStrokeShare = 0.1f;
    const float GlyphShare = 0.94f; // the headset fills this share of the icon's width
    const int PlateTextMargin = 1; // inside the plate's rounded edge; a plain number uses the full width
    const int MinFontHeight = 6;
    const uint TextOnMask = 0xFFFFFF;

    /// <summary>Creates an icon the caller must destroy, or 0 if drawing failed.</summary>
    public static nint Create(in IconDescription description, int size)
    {
        nint color = CreateBitmapBuffer(size, out uint* pixels);
        if (color == 0)
            return 0;

        nint icon = 0;
        if (Render(description, size, pixels))
        {
            nint mask = CreateEmptyMask(size);
            var iconInfo = new ICONINFO { fIcon = 1, hbmMask = mask, hbmColor = color };
            icon = mask != 0 ? CreateIconIndirect(&iconInfo) : 0;

            // CreateIconIndirect copies the bitmaps.
            if (mask != 0)
                DeleteObject(mask);
        }
        DeleteObject(color);
        return icon;
    }

    /// <summary>Draws into <paramref name="pixels"/>: size × size, top-down, 0xAARRGGBB with straight alpha.</summary>
    public static bool Render(in IconDescription description, int size, uint* pixels)
    {
        switch (description.Shape)
        {
            case IconShape.Plate:
                DrawSquare(description.Color, size, pixels, isHollow: false);
                break;
            case IconShape.Hollow:
                DrawSquare(description.Color, size, pixels, isHollow: true);
                break;
            case IconShape.Headset:
            case IconShape.HeadsetSlashed:
                DrawHeadset(description.Color, size, pixels, description.Shape == IconShape.HeadsetSlashed);
                break;
            default: // IconShape.Number: the number alone, on transparent pixels
                new Span<uint>(pixels, size * size).Clear();
                break;
        }

        return description.Number == IconDescription.NoNumber ||
            DrawNumber(description.Number.ToString(), description.TextColor, size, pixels,
                description.Shape == IconShape.Number ? 0 : PlateTextMargin);
    }

    static void DrawSquare(uint color, int size, uint* pixels, bool isHollow)
    {
        float half = size / 2f;
        float radius = size * CornerShare;
        float stroke = Math.Max(1f, size * HollowStrokeShare);
        color &= 0xFFFFFF;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Shapes.RoundedBox(x + 0.5f, y + 0.5f, half, half, half, half, radius);
                float coverage = Shapes.Coverage(distance);
                if (isHollow)
                    coverage -= Shapes.Coverage(distance + stroke);
                pixels[y * size + x] = ToPixel(coverage, color);
            }
        }
    }

    /// <summary>The logo's headset, scaled from its 256-unit grid to fit and centered.</summary>
    static void DrawHeadset(uint color, int size, uint* pixels, bool isSlashed)
    {
        const float Width = Shapes.HeadsetRight - Shapes.HeadsetLeft;
        const float CenterX = (Shapes.HeadsetLeft + Shapes.HeadsetRight) / 2f;
        const float CenterY = (Shapes.HeadsetTop + Shapes.HeadsetBottom) / 2f;
        float scale = size * GlyphShare / Width; // pixels per grid unit
        float half = size / 2f;
        color &= 0xFFFFFF;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float gridX = CenterX + (x + 0.5f - half) / scale;
                float gridY = CenterY + (y + 0.5f - half) / scale;
                float distance = isSlashed ? Shapes.HeadsetSlashed(gridX, gridY) : Shapes.Headset(gridX, gridY);
                pixels[y * size + x] = ToPixel(Shapes.Coverage(distance * scale), color);
            }
        }
    }

    static uint ToPixel(float coverage, uint color) => ((uint)(Math.Clamp(coverage, 0f, 1f) * 255f + 0.5f) << 24) | color;

    static bool DrawNumber(string text, uint textColor, int size, uint* pixels, int margin)
    {
        nint dc = CreateCompatibleDC(0);
        if (dc == 0)
            return false;

        nint maskBitmap = CreateBitmapBuffer(size, out uint* mask);
        nint previousBitmap = maskBitmap != 0 ? SelectObject(dc, maskBitmap) : 0;
        nint font = maskBitmap != 0 ? CreateFittingFont(dc, text, size, margin) : 0;
        bool drawn = font != 0;
        if (drawn)
        {
            nint previousFont = SelectObject(dc, font);
            SetBkMode(dc, TRANSPARENT);
            SetTextColor(dc, TextOnMask);
            var bounds = new RECT { Right = size, Bottom = size };
            DrawTextW(dc, text, -1, &bounds, DT_CENTER | DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX);
            GdiFlush(); // GDI batches its calls, and the mask is read directly below
            Blend(mask, pixels, size * size, textColor);
            SelectObject(dc, previousFont);
            DeleteObject(font);
        }

        if (maskBitmap != 0)
        {
            SelectObject(dc, previousBitmap);
            DeleteObject(maskBitmap);
        }
        DeleteDC(dc);
        return drawn;
    }

    /// <summary>The largest bold system font whose text fits, so "100" fits at every size.</summary>
    static nint CreateFittingFont(nint dc, string text, int size, int margin)
    {
        LOGFONTW logFont = SystemFont.GetMessageFont();
        logFont.lfWeight = FW_BOLD;
        logFont.lfQuality = ANTIALIASED_QUALITY;

        int available = size - 2 * margin;
        for (int height = size; height >= MinFontHeight; height--)
        {
            logFont.lfHeight = -height;
            nint font = CreateFontIndirectW(&logFont);
            if (font == 0)
                return 0;

            nint previous = SelectObject(dc, font);
            SIZE extent = default;
            bool fits = GetTextExtentPoint32W(dc, text, text.Length, &extent) && extent.cx <= available;
            SelectObject(dc, previous);
            if (fits || height == MinFontHeight)
                return font;
            DeleteObject(font);
        }
        return 0;
    }

    /// <summary>Paints the text color over the pixels, with the mask as coverage ("over" in straight alpha).</summary>
    static void Blend(uint* mask, uint* pixels, int count, uint textColor)
    {
        for (int i = 0; i < count; i++)
        {
            uint coverage = mask[i] & 0xFF;
            if (coverage == 0)
                continue;

            uint pixel = pixels[i];
            uint below = (pixel >> 24) * (255 - coverage) / 255; // what still shows of the pixel
            uint alpha = coverage + below;
            uint red = Over(textColor >> 16, pixel >> 16, coverage, below, alpha);
            uint green = Over(textColor >> 8, pixel >> 8, coverage, below, alpha);
            uint blue = Over(textColor, pixel, coverage, below, alpha);
            pixels[i] = (alpha << 24) | (red << 16) | (green << 8) | blue;
        }
    }

    static uint Over(uint top, uint bottom, uint topWeight, uint bottomWeight, uint alpha) =>
        ((top & 0xFF) * topWeight + (bottom & 0xFF) * bottomWeight + alpha / 2) / alpha;

    static nint CreateBitmapBuffer(int size, out uint* pixels)
    {
        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = size,
            biHeight = -size, // top-down
            biPlanes = 1,
            biBitCount = 32,
            biCompression = BI_RGB,
        };

        void* bits;
        nint bitmap = CreateDIBSection(0, &header, DIB_RGB_COLORS, &bits, 0, 0);
        pixels = (uint*)bits;
        return bits != null ? bitmap : 0;
    }

    /// <summary>All-zero monochrome mask (rows aligned to 16 bits); the alpha channel rules.</summary>
    static nint CreateEmptyMask(int size)
    {
        int stride = ((size + 15) >> 4) << 1;
        int length = stride * size;
        byte* bits = stackalloc byte[length];
        new Span<byte>(bits, length).Clear();
        return CreateBitmap(size, size, 1, 1, bits);
    }
}
