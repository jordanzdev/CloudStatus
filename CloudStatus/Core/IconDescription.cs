namespace CloudStatus.Core;

/// <summary>
/// What the tray icon shows, decided without drawing anything. Colors are 0xRRGGBB. Two equal
/// descriptions look the same, so the icon is only redrawn when this value changes.
/// </summary>
internal readonly record struct IconDescription(int Number, uint Background, uint TextColor, bool IsHollow)
{
    public const int NoNumber = -1;
    public const int LowLimit = 20;
    public const int MediumLimit = 50;

    const uint Teal = 0x26C6A6;
    const uint Dark = 0x2B313D;
    const uint Amber = 0xF9A825;
    const uint Red = 0xE53935;
    const uint Grey = 0x6B7280;
    const uint Black = 0x000000;
    const uint White = 0xFFFFFF;

    public static IconDescription Describe(HeadsetPower power, byte percent, ChargeState charge) => power switch
    {
        HeadsetPower.On => WithNumber(percent, BatteryBackground(percent, charge)),
        HeadsetPower.Checking => WithoutNumber(Dark, isHollow: false),
        HeadsetPower.Off => WithoutNumber(Grey, isHollow: false),
        _ => WithoutNumber(Grey, isHollow: true),
    };

    /// <summary>WCAG contrast ratio between two colors, from 1 to 21.</summary>
    public static float ContrastRatio(uint first, uint second)
    {
        float lighter = Math.Max(Luminance(first), Luminance(second));
        float darker = Math.Min(Luminance(first), Luminance(second));
        return (lighter + 0.05f) / (darker + 0.05f);
    }

    static uint BatteryBackground(byte percent, ChargeState charge)
    {
        if (charge == ChargeState.Error)
            return Red;
        if (charge is ChargeState.Charging or ChargeState.Full)
            return Teal; // it is being fixed, so a low level no longer needs attention
        if (percent < LowLimit)
            return Red;
        return percent < MediumLimit ? Amber : Dark;
    }

    static IconDescription WithNumber(byte percent, uint background) =>
        new(percent, background, ReadableTextColor(background), false);

    static IconDescription WithoutNumber(uint background, bool isHollow) =>
        new(NoNumber, background, ReadableTextColor(background), isHollow);

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
