namespace CloudStatus.Ui.Drawing;

/// <summary>
/// Signed distances to simple shapes, negative inside, and their anti-aliased coverage. Everything
/// CloudStatus draws (tray icon, toggles, cards, buttons) is built from these, with no image files.
/// </summary>
internal static class Shapes
{
    /// <summary>Pixel coverage of a point at <paramref name="distance"/> pixels from an edge.</summary>
    public static float Coverage(float distance) => Math.Clamp(0.5f - distance, 0f, 1f);

    /// <summary>Rounded rectangle centered at (<paramref name="centerX"/>, <paramref name="centerY"/>).</summary>
    public static float RoundedBox(float x, float y, float centerX, float centerY, float halfWidth, float halfHeight, float radius)
    {
        radius = Math.Min(radius, Math.Min(halfWidth, halfHeight));
        float cornerX = Math.Abs(x - centerX) - (halfWidth - radius);
        float cornerY = Math.Abs(y - centerY) - (halfHeight - radius);
        float outsideX = Math.Max(cornerX, 0f), outsideY = Math.Max(cornerY, 0f);
        return MathF.Sqrt(outsideX * outsideX + outsideY * outsideY) + Math.Min(Math.Max(cornerX, cornerY), 0f) - radius;
    }

    /// <summary>Rounded rectangle given by its edges.</summary>
    public static float RoundedRect(float x, float y, float left, float top, float right, float bottom, float radius) =>
        RoundedBox(x, y, (left + right) / 2f, (top + bottom) / 2f, (right - left) / 2f, (bottom - top) / 2f, radius);

    public static float Circle(float x, float y, float centerX, float centerY, float radius)
    {
        float dx = x - centerX, dy = y - centerY;
        return MathF.Sqrt(dx * dx + dy * dy) - radius;
    }

    /// <summary>Segment with round caps, <paramref name="halfWidth"/> on each side.</summary>
    public static float Segment(float x, float y, float x0, float y0, float x1, float y1, float halfWidth)
    {
        float px = x - x0, py = y - y0, dx = x1 - x0, dy = y1 - y0;
        float lengthSquared = dx * dx + dy * dy;
        float t = lengthSquared > 0f ? Math.Clamp((px * dx + py * dy) / lengthSquared, 0f, 1f) : 0f;
        float ex = px - dx * t, ey = py - dy * t;
        return MathF.Sqrt(ex * ex + ey * ey) - halfWidth;
    }

    /// <summary>
    /// The logo's headset in the bold variant tools/New-Branding.ps1 uses up to 24 px, on its 256-unit
    /// grid: an arc for the band and two rounded cups.
    /// </summary>
    public static float Headset(float x, float y)
    {
        const float CenterX = 128f, ArcY = 124f, ArcRadius = 90f, HalfStroke = 15f;
        const float CupLeft = 16f, CupWidth = 46f, CupTop = 118f, CupHeight = 98f, CupRadius = 14f;

        float band;
        if (y <= ArcY)
            band = Math.Abs(Circle(x, y, CenterX, ArcY, ArcRadius)) - HalfStroke;
        else // below the arc's ends, which the cups cover
            band = Math.Min(Circle(x, y, CenterX - ArcRadius, ArcY, HalfStroke), Circle(x, y, CenterX + ArcRadius, ArcY, HalfStroke));

        float leftCup = RoundedRect(x, y, CupLeft, CupTop, CupLeft + CupWidth, CupTop + CupHeight, CupRadius);
        float rightCup = RoundedRect(x, y, 256f - CupLeft - CupWidth, CupTop, 256f - CupLeft, CupTop + CupHeight, CupRadius);
        return Math.Min(band, Math.Min(leftCup, rightCup));
    }

    /// <summary>The headset's bounds on the 256-unit grid, used to fit it into an icon.</summary>
    public const float HeadsetLeft = 16f, HeadsetTop = 19f, HeadsetRight = 240f, HeadsetBottom = 216f;

    /// <summary>The headset crossed out from corner to corner, with a gap cut around the slash.</summary>
    public static float HeadsetSlashed(float x, float y)
    {
        const float HalfSlash = 12f, Gap = 12f;
        float slash = Segment(x, y, HeadsetLeft, HeadsetTop - 4f, HeadsetRight, HeadsetBottom + 4f, HalfSlash);
        float carved = Math.Max(Headset(x, y), -(slash - Gap));
        return Math.Min(carved, slash);
    }
}
