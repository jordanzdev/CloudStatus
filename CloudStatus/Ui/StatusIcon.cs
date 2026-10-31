using CloudStatus.Core;
using CloudStatus.Native;
using static CloudStatus.Native.Gdi32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>
/// Draws the tray icon in memory: a rounded square with the battery number. The square is computed
/// pixel by pixel in straight alpha. GDI text does not write alpha, so the number is drawn on a
/// separate mask and its brightness becomes coverage.
/// </summary>
internal static unsafe class StatusIcon
{
    const float CornerShare = 0.2f;
    const float HollowStrokeShare = 0.1f;
    const int TextMargin = 1;
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
        DrawSquare(description, size, pixels);
        return description.Number == IconDescription.NoNumber ||
            DrawNumber(description.Number.ToString(), description, size, pixels);
    }

    static void DrawSquare(in IconDescription description, int size, uint* pixels)
    {
        float half = size / 2f;
        float radius = size * CornerShare;
        float stroke = Math.Max(1f, size * HollowStrokeShare);
        uint background = description.Background & 0xFFFFFF;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = DistanceToEdge(x + 0.5f - half, y + 0.5f - half, half, radius);
                float coverage = Coverage(distance);
                if (description.IsHollow)
                    coverage -= Coverage(distance + stroke);
                pixels[y * size + x] = ((uint)(coverage * 255f + 0.5f) << 24) | background;
            }
        }
    }

    /// <summary>Signed distance to the rounded square: negative inside.</summary>
    static float DistanceToEdge(float x, float y, float half, float radius)
    {
        float cornerX = Math.Abs(x) - (half - radius);
        float cornerY = Math.Abs(y) - (half - radius);
        float outsideX = Math.Max(cornerX, 0f), outsideY = Math.Max(cornerY, 0f);
        return MathF.Sqrt(outsideX * outsideX + outsideY * outsideY) + Math.Min(Math.Max(cornerX, cornerY), 0f) - radius;
    }

    static float Coverage(float distance) => Math.Clamp(0.5f - distance, 0f, 1f);

    static bool DrawNumber(string text, in IconDescription description, int size, uint* pixels)
    {
        nint dc = CreateCompatibleDC(0);
        if (dc == 0)
            return false;

        nint maskBitmap = CreateBitmapBuffer(size, out uint* mask);
        nint previousBitmap = maskBitmap != 0 ? SelectObject(dc, maskBitmap) : 0;
        nint font = maskBitmap != 0 ? CreateFittingFont(dc, text, size) : 0;
        bool drawn = font != 0;
        if (drawn)
        {
            nint previousFont = SelectObject(dc, font);
            SetBkMode(dc, TRANSPARENT);
            SetTextColor(dc, TextOnMask);
            var bounds = new RECT { Right = size, Bottom = size };
            DrawTextW(dc, text, -1, &bounds, DT_CENTER | DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX);
            GdiFlush(); // GDI batches its calls, and the mask is read directly below
            Blend(mask, pixels, size * size, description.TextColor);
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
    static nint CreateFittingFont(nint dc, string text, int size)
    {
        LOGFONTW logFont = SystemFont.GetMessageFont();
        logFont.lfWeight = FW_BOLD;
        logFont.lfQuality = ANTIALIASED_QUALITY;

        int available = size - 2 * TextMargin;
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

    static void Blend(uint* mask, uint* pixels, int count, uint textColor)
    {
        for (int i = 0; i < count; i++)
        {
            uint coverage = mask[i] & 0xFF;
            if (coverage == 0)
                continue;

            uint pixel = pixels[i];
            uint alpha = Math.Max(pixel >> 24, coverage);
            uint red = Mix(pixel >> 16, textColor >> 16, coverage);
            uint green = Mix(pixel >> 8, textColor >> 8, coverage);
            uint blue = Mix(pixel, textColor, coverage);
            pixels[i] = (alpha << 24) | (red << 16) | (green << 8) | blue;
        }
    }

    static uint Mix(uint from, uint to, uint amount) => ((from & 0xFF) * (255 - amount) + (to & 0xFF) * amount + 127) / 255;

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
