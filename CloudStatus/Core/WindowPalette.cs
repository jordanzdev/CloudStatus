namespace CloudStatus.Core;

/// <summary>
/// Colors of the settings window (spec 013, REQ-013-05), as 0xRRGGBB. High contrast does not use
/// these: the window builds a palette from the system colors instead.
/// </summary>
internal readonly record struct WindowPalette(
    uint Background,
    uint Card,
    uint CardBorder,
    uint Text,
    uint SecondaryText,
    uint Accent,
    uint AccentText,
    uint Error,
    uint WarningBar,
    uint WarningIcon,
    uint Control,
    uint ControlBorder,
    uint ControlStrongBorder)
{
    public static readonly WindowPalette Light = new(
        Background: 0xF3F3F3,
        Card: 0xFBFBFB,
        CardBorder: 0xE5E5E5,
        Text: 0x1A1A1A,
        SecondaryText: 0x5D5D5D,
        Accent: 0x007A66,
        AccentText: 0xFFFFFF,
        Error: 0xC42B1C,
        WarningBar: 0xFFF4CE,
        WarningIcon: 0x9D5D00,
        Control: 0xFFFFFF,
        ControlBorder: 0xE5E5E5,
        ControlStrongBorder: 0x868686);

    public static readonly WindowPalette Dark = new(
        Background: 0x202020,
        Card: 0x2B2B2B,
        CardBorder: 0x1D1D1D,
        Text: 0xFFFFFF,
        SecondaryText: 0xC5C5C5,
        Accent: 0x26C6A6,
        AccentText: 0x000000,
        Error: 0xFF99A4,
        WarningBar: 0x433519,
        WarningIcon: 0xFCE100,
        Control: 0x373737,
        ControlBorder: 0x454545,
        ControlStrongBorder: 0x9A9A9A);

    public static WindowPalette For(bool isDark) => isDark ? Dark : Light;
}
