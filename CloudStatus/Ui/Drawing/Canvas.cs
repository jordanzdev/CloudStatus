using CloudStatus.Native;
using static CloudStatus.Native.Gdi32;

namespace CloudStatus.Ui.Drawing;

/// <summary>
/// An opaque off-screen 32 bpp bitmap with a DC, for flicker-free painting with anti-aliased
/// shapes. Shapes are blended pixel by pixel; text is drawn with GDI on <see cref="Dc"/>. Colors
/// are 0xRRGGBB. Every GDI object is released by <see cref="Dispose"/>.
/// </summary>
internal unsafe struct Canvas : IDisposable
{
    nint _bitmap;
    nint _previousBitmap;
    uint* _pixels;

    public nint Dc { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public static bool TryCreate(int width, int height, out Canvas canvas)
    {
        canvas = default;
        if (width <= 0 || height <= 0)
            return false;

        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = width,
            biHeight = -height, // top-down
            biPlanes = 1,
            biBitCount = 32,
            biCompression = BI_RGB,
        };
        void* bits;
        nint bitmap = CreateDIBSection(0, &header, DIB_RGB_COLORS, &bits, 0, 0);
        if (bitmap == 0 || bits == null)
            return false;

        nint dc = CreateCompatibleDC(0);
        if (dc == 0)
        {
            DeleteObject(bitmap);
            return false;
        }

        canvas = new Canvas
        {
            _bitmap = bitmap,
            _previousBitmap = SelectObject(dc, bitmap),
            _pixels = (uint*)bits,
            Dc = dc,
            Width = width,
            Height = height,
        };
        return true;
    }

    public readonly void Clear(uint color)
    {
        GdiFlush();
        new Span<uint>(_pixels, Width * Height).Fill(color & 0xFFFFFF);
    }

    public readonly void FillRoundedRect(in RECT rect, float radius, uint color) =>
        FillRoundedRect(rect.Left, rect.Top, rect.Right, rect.Bottom, radius, color);

    public readonly void FillRoundedRect(float left, float top, float right, float bottom, float radius, uint color)
    {
        GdiFlush();
        Bounds(left, top, right, bottom, out int x0, out int y0, out int x1, out int y1);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                Blend(x, y, color, Shapes.Coverage(Shapes.RoundedRect(x + 0.5f, y + 0.5f, left, top, right, bottom, radius)));
    }

    /// <summary>A border of <paramref name="width"/> pixels inside the rectangle's edge.</summary>
    public readonly void StrokeRoundedRect(in RECT rect, float radius, float width, uint color) =>
        StrokeRoundedRect(rect.Left, rect.Top, rect.Right, rect.Bottom, radius, width, color);

    public readonly void StrokeRoundedRect(float left, float top, float right, float bottom, float radius, float width, uint color)
    {
        GdiFlush();
        Bounds(left, top, right, bottom, out int x0, out int y0, out int x1, out int y1);
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                float distance = Shapes.RoundedRect(x + 0.5f, y + 0.5f, left, top, right, bottom, radius);
                Blend(x, y, color, Shapes.Coverage(distance) - Shapes.Coverage(distance + width));
            }
        }
    }

    /// <summary>The part of a rounded rectangle at or below <paramref name="bandTop"/>: the field's bottom line.</summary>
    public readonly void FillRoundedRectBottom(in RECT rect, float radius, int bandTop, uint color)
    {
        GdiFlush();
        Bounds(rect.Left, Math.Max(rect.Top, bandTop), rect.Right, rect.Bottom, out int x0, out int y0, out int x1, out int y1);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                Blend(x, y, color, Shapes.Coverage(Shapes.RoundedRect(x + 0.5f, y + 0.5f, rect.Left, rect.Top, rect.Right, rect.Bottom, radius)));
    }

    public readonly void FillCircle(float centerX, float centerY, float radius, uint color)
    {
        GdiFlush();
        Bounds(centerX - radius, centerY - radius, centerX + radius, centerY + radius, out int x0, out int y0, out int x1, out int y1);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                Blend(x, y, color, Shapes.Coverage(Shapes.Circle(x + 0.5f, y + 0.5f, centerX, centerY, radius)));
    }

    public readonly void FillSegment(float x0, float y0, float x1, float y1, float halfWidth, uint color)
    {
        GdiFlush();
        Bounds(Math.Min(x0, x1) - halfWidth, Math.Min(y0, y1) - halfWidth, Math.Max(x0, x1) + halfWidth, Math.Max(y0, y1) + halfWidth,
            out int left, out int top, out int right, out int bottom);
        for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
                Blend(x, y, color, Shapes.Coverage(Shapes.Segment(x + 0.5f, y + 0.5f, x0, y0, x1, y1, halfWidth)));
    }

    /// <summary>Copies the whole canvas to <paramref name="dc"/> at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public readonly void CopyTo(nint dc, int x, int y) => BitBlt(dc, x, y, Width, Height, Dc, 0, 0, SRCCOPY);

    public void Dispose()
    {
        if (Dc != 0)
        {
            SelectObject(Dc, _previousBitmap);
            DeleteDC(Dc);
        }
        if (_bitmap != 0)
            DeleteObject(_bitmap);
        this = default;
    }

    /// <summary>Mixes two 0xRRGGBB colors: 0 gives <paramref name="from"/>, 1 gives <paramref name="to"/>.</summary>
    public static uint Mix(uint from, uint to, float amount)
    {
        uint weight = (uint)(Math.Clamp(amount, 0f, 1f) * 255f + 0.5f);
        return (MixChannel(from >> 16, to >> 16, weight) << 16) | (MixChannel(from >> 8, to >> 8, weight) << 8) | MixChannel(from, to, weight);
    }

    /// <summary>GDI's COLORREF (0x00BBGGRR) from 0xRRGGBB, and back: the swap is its own inverse.</summary>
    public static uint ToColorRef(uint color) => ((color & 0xFF) << 16) | (color & 0xFF00) | ((color >> 16) & 0xFF);

    readonly void Bounds(float left, float top, float right, float bottom, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = Math.Max(0, (int)MathF.Floor(left));
        y0 = Math.Max(0, (int)MathF.Floor(top));
        x1 = Math.Min(Width, (int)MathF.Ceiling(right));
        y1 = Math.Min(Height, (int)MathF.Ceiling(bottom));
    }

    readonly void Blend(int x, int y, uint color, float coverage)
    {
        if (coverage <= 0f)
            return;
        uint* pixel = _pixels + y * Width + x;
        *pixel = coverage >= 1f ? color & 0xFFFFFF : Mix(*pixel, color, coverage);
    }

    static uint MixChannel(uint from, uint to, uint weight) => ((from & 0xFF) * (255 - weight) + (to & 0xFF) * weight + 127) / 255;
}
