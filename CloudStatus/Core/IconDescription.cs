namespace CloudStatus.Core;

/// <summary>How the tray icon is drawn (spec 013, REQ-013-17).</summary>
internal enum IconShape : byte
{
    /// <summary>The number on a filled rounded square of <see cref="IconDescription.Color"/>.</summary>
    Plate,
    /// <summary>The number alone, in <see cref="IconDescription.Color"/>, like a system icon.</summary>
    Number,
    /// <summary>The outline of the rounded square, no number.</summary>
    Hollow,
    /// <summary>The logo's headset: band and cups.</summary>
    Headset,
    /// <summary>The headset crossed out.</summary>
    HeadsetSlashed,
}

/// <summary>
/// What the tray icon shows, decided without drawing anything. Colors are 0xRRGGBB:
/// <see cref="Color"/> fills the plate or draws the number and glyphs, <see cref="TextColor"/> is
/// the number on a plate. Two equal descriptions look the same, so the icon is only redrawn when
/// this value changes.
/// </summary>
internal readonly record struct IconDescription(int Number, uint Color, uint TextColor, IconShape Shape)
{
    public const int NoNumber = -1;
    public const byte DefaultLowLimit = 20; // a setting since spec 007
    public const int MediumLimit = 50;

    public const uint Teal = 0x26C6A6;
    public const uint Dark = 0x2B313D;
    public const uint Amber = 0xF9A825;
    public const uint Red = 0xE53935;
    public const uint Grey = 0x6B7280;
    public const uint DarkTaskbarForeground = 0xFFFFFF;
    public const uint LightTaskbarForeground = 0x1A1A1A;
    const uint Black = 0x000000;
    const uint White = 0xFFFFFF;

    public static IconDescription Describe(HeadsetPower power, byte percent, ChargeState charge, byte lowLimit, in Theme theme) =>
        theme.IsHighContrast
            ? DescribeWithPlates(power, percent, charge, lowLimit)
            : DescribeForTaskbar(power, percent, charge, lowLimit, theme.IsTaskbarDark ? DarkTaskbarForeground : LightTaskbarForeground);

    /// <summary>WCAG contrast ratio between two colors, from 1 to 21.</summary>
    public static float ContrastRatio(uint first, uint second)
    {
        float lighter = Math.Max(Luminance(first), Luminance(second));
        float darker = Math.Min(Luminance(first), Luminance(second));
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    /// <summary>Plates only draw attention; a healthy battery and the other states use the taskbar's own color.</summary>
    static IconDescription DescribeForTaskbar(HeadsetPower power, byte percent, ChargeState charge, byte lowLimit, uint foreground)
    {
        switch (power)
        {
            case HeadsetPower.On:
                uint plate = BatteryBackground(percent, charge, lowLimit);
                return plate == Dark
                    ? new IconDescription(percent, foreground, foreground, IconShape.Number)
                    : new IconDescription(percent, plate, ReadableTextColor(plate), IconShape.Plate);
            case HeadsetPower.Checking:
                return Glyph(foreground, IconShape.Hollow);
            case HeadsetPower.Off:
                return Glyph(foreground, IconShape.Headset);
            default:
                return Glyph(foreground, IconShape.HeadsetSlashed);
        }
    }

    /// <summary>High contrast keeps spec 005's style: every state on its own plate.</summary>
    static IconDescription DescribeWithPlates(HeadsetPower power, byte percent, ChargeState charge, byte lowLimit) => power switch
    {
        HeadsetPower.On => WithNumber(percent, BatteryBackground(percent, charge, lowLimit)),
        HeadsetPower.Checking => Glyph(Dark, IconShape.Plate),
        HeadsetPower.Off => Glyph(Grey, IconShape.Plate),
        _ => Glyph(Grey, IconShape.Hollow),
    };

    static uint BatteryBackground(byte percent, ChargeState charge, byte lowLimit)
    {
        if (charge == ChargeState.Error)
            return Red;
        if (charge is ChargeState.Charging or ChargeState.Full)
            return Teal; // it is being fixed, so a low level no longer needs attention
        if (percent < lowLimit)
            return Red;
        return percent < MediumLimit ? Amber : Dark;
    }

    static IconDescription WithNumber(byte percent, uint background) =>
        new(percent, background, ReadableTextColor(background), IconShape.Plate);

    static IconDescription Glyph(uint color, IconShape shape) =>
        new(NoNumber, color, ReadableTextColor(color), shape);

    static uint ReadableTextColor(uint background) =>
        ContrastRatio(background, White) >= ContrastRatio(background, Black) ? White : Black;

    static float Luminance(uint color) =>
        0.2126f * Linear(color >> 16) + 0.7152f * Linear(color >> 8) + 0.0722f * Linear(color);

    static float Linear(uint channel)
    {
        float value = (channel & 0xFF) / 255f;
        return value <= 0.04045f ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
    }
}
