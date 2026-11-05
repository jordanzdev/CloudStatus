using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class WindowPaletteTests
{
    const float TextContrast = 4.5f;
    const float NonTextContrast = 3f;

    public static TheoryData<bool> Themes => new() { false, true };

    [Theory] // AC-013-02: text on every surface it is drawn on
    [MemberData(nameof(Themes))]
    public void Text_IsReadableOnEverySurface(bool isDark)
    {
        WindowPalette p = WindowPalette.For(isDark);

        AssertContrast(p.Text, p.Background, TextContrast);   // section headings
        AssertContrast(p.Text, p.Card, TextContrast);         // row labels, buttons
        AssertContrast(p.Text, p.Control, TextContrast);      // number fields
        AssertContrast(p.Text, p.WarningBar, TextContrast);   // info bar
        AssertContrast(p.SecondaryText, p.Card, TextContrast);
        AssertContrast(p.SecondaryText, p.Background, TextContrast);
        AssertContrast(p.Error, p.Card, TextContrast);
        AssertContrast(p.AccentText, p.Accent, TextContrast); // Save, the info icon
        AssertContrast(p.WarningBar, p.WarningIcon, TextContrast); // the "!" on the warning icon
    }

    [Theory] // AC-013-02: the accent and the icons as shapes
    [MemberData(nameof(Themes))]
    public void Accent_StandsOutFromTheCard(bool isDark)
    {
        WindowPalette p = WindowPalette.For(isDark);

        AssertContrast(p.Accent, p.Card, NonTextContrast);
        AssertContrast(p.Accent, p.Background, NonTextContrast);
        AssertContrast(p.WarningIcon, p.WarningBar, NonTextContrast);
        AssertContrast(p.SecondaryText, p.Card, NonTextContrast); // the off toggle's outline
    }

    [Fact] // REQ-013-05
    public void Palettes_MatchTheSpec()
    {
        Assert.Equal(0xF3F3F3u, WindowPalette.Light.Background);
        Assert.Equal(0x202020u, WindowPalette.Dark.Background);
        Assert.Equal(0x007A66u, WindowPalette.Light.Accent);
        Assert.Equal(0x26C6A6u, WindowPalette.Dark.Accent);
        Assert.Equal(WindowPalette.Dark, WindowPalette.For(true));
        Assert.Equal(WindowPalette.Light, WindowPalette.For(false));
    }

    static void AssertContrast(uint foreground, uint background, float minimum)
    {
        float ratio = IconDescription.ContrastRatio(foreground, background);
        Assert.True(ratio >= minimum, $"{foreground:X6} on {background:X6} is {ratio:0.00}:1, below {minimum}:1");
    }
}
