using CloudStatus.Core;
using CloudStatus.Native;
using CloudStatus.Ui.Drawing;
using static CloudStatus.Native.Comctl32;
using static CloudStatus.Native.Gdi32;
using static CloudStatus.Native.User32;
using static CloudStatus.Ui.Settings.SettingsControls;

namespace CloudStatus.Ui.Settings;

/// <summary>
/// Paints the settings window in the Windows 11 style (spec 013): background, row cards, number
/// field boxes and the info bar in WM_PAINT; toggles and buttons through NM_CUSTOMDRAW; text colors
/// through WM_CTLCOLOR*. Its brushes exist only while the window is open (REQ-013-14).
/// </summary>
internal static unsafe class SettingsPainter
{
    const int CornerRadius = 4;      // at 96 DPI, as Windows 11 controls
    const int TrackWidth = 40, TrackHeight = 20, ThumbRadius = 5, ThumbHotRadius = 6;
    const int FocusRingGap = 3, FocusRingWidth = 2;
    const int InfoIconRadius = 8, InfoIconLeft = 16, InfoIconTop = 16;
    const int TextCapacity = 128;

    static WindowPalette s_palette = WindowPalette.Light;
    static nint s_backgroundBrush;
    static nint s_cardBrush;
    static nint s_controlBrush;
    static nint s_infoBrush;

    /// <summary>The info bar shows a warning (true) or a plain result (false).</summary>
    public static bool IsInfoWarning { get; set; }

    /// <summary>The secondary text that currently shows an error instead of its range, or 0.</summary>
    public static int ErrorTextId { get; set; }

    public static WindowPalette Palette => s_palette;

    /// <summary>Builds the palette and brushes for the current theme, replacing any previous ones.</summary>
    public static void ApplyTheme()
    {
        Theme theme = SystemTheme.Current;
        s_palette = theme.IsHighContrast ? SystemPalette() : WindowPalette.For(theme.IsWindowDark);
        Release();
        s_backgroundBrush = CreateSolidBrush(Canvas.ToColorRef(s_palette.Background));
        s_cardBrush = CreateSolidBrush(Canvas.ToColorRef(s_palette.Card));
        s_controlBrush = CreateSolidBrush(Canvas.ToColorRef(s_palette.Control));
        UpdateInfoBrush();
    }

    /// <summary>The info bar's color depends on its kind of message.</summary>
    public static void UpdateInfoBrush()
    {
        if (s_infoBrush != 0)
            DeleteObject(s_infoBrush);
        s_infoBrush = CreateSolidBrush(Canvas.ToColorRef(InfoBarColor));
    }

    public static void Release()
    {
        DeleteIfCreated(ref s_backgroundBrush);
        DeleteIfCreated(ref s_cardBrush);
        DeleteIfCreated(ref s_controlBrush);
        DeleteIfCreated(ref s_infoBrush);
    }

    static uint InfoBarColor => IsInfoWarning ? s_palette.WarningBar : s_palette.Card;

    /// <summary>High contrast: every surface from the system colors (REQ-013-04).</summary>
    static WindowPalette SystemPalette()
    {
        uint window = SystemColor(COLOR_WINDOW), text = SystemColor(COLOR_WINDOWTEXT);
        return new WindowPalette(
            Background: window,
            Card: window,
            CardBorder: text,
            Text: text,
            SecondaryText: text,
            Accent: SystemColor(COLOR_HIGHLIGHT),
            AccentText: SystemColor(COLOR_HIGHLIGHTTEXT),
            Error: text,
            WarningBar: window,
            WarningIcon: text,
            Control: window,
            ControlBorder: text,
            ControlStrongBorder: text);
    }

    static uint SystemColor(int index) => Canvas.ToColorRef(GetSysColor(index)); // COLORREF to 0xRRGGBB

    public static void Paint(nint window)
    {
        PAINTSTRUCT paint;
        nint dc = BeginPaint(window, &paint);
        if (dc == 0)
            return;

        RECT client;
        if (GetClientRect(window, &client) && Canvas.TryCreate(client.Width, client.Height, out Canvas canvas))
        {
            float radius = Dpi.Scale(CornerRadius, GetDpiForWindow(window));
            canvas.Clear(s_palette.Background);
            if (HasInfoBar)
                PaintInfoBar(ref canvas, GetDpiForWindow(window), radius);
            PaintRows(ref canvas, window, radius);
            canvas.CopyTo(dc, 0, 0);
            canvas.Dispose();
        }
        EndPaint(window, &paint);
    }

    static void PaintRows(ref Canvas canvas, nint window, float radius)
    {
        nint focus = GetFocus();
        ReadOnlySpan<RECT> cards = Cards, boxes = NumberBoxes;
        for (int i = 0; i < cards.Length; i++)
        {
            canvas.FillRoundedRect(cards[i], radius, s_palette.Card);
            canvas.StrokeRoundedRect(cards[i], radius, 1f, s_palette.CardBorder);
            if (Row(i).Kind != RowKind.Number)
                continue;

            // A Windows 11 text box: border, and a bottom line that turns into the accent with focus.
            RECT box = boxes[i];
            bool isFocused = focus != 0 && focus == Get(window, Row(i).ControlId);
            canvas.FillRoundedRect(box, radius, s_palette.Control);
            canvas.StrokeRoundedRect(box, radius, 1f, s_palette.ControlBorder);
            int lineHeight = isFocused ? Math.Max(2, Dpi.Scale(2, GetDpiForWindow(window))) : 1;
            canvas.FillRoundedRectBottom(box, radius, box.Bottom - lineHeight, isFocused ? s_palette.Accent : s_palette.ControlStrongBorder);
        }
    }

    static void PaintInfoBar(ref Canvas canvas, uint dpi, float radius)
    {
        RECT bar = InfoBar;
        canvas.FillRoundedRect(bar, radius, InfoBarColor);
        canvas.StrokeRoundedRect(bar, radius, 1f, s_palette.CardBorder);

        float r = Dpi.Scale(InfoIconRadius, dpi);
        float cx = bar.Left + Dpi.Scale(InfoIconLeft, dpi) + r;
        float cy = bar.Top + Dpi.Scale(InfoIconTop, dpi) + r;
        uint glyph = IsInfoWarning ? s_palette.WarningBar : s_palette.AccentText;
        canvas.FillCircle(cx, cy, r, IsInfoWarning ? s_palette.WarningIcon : s_palette.Accent);
        float stroke = r * 0.13f;
        if (IsInfoWarning) // "!"
        {
            canvas.FillSegment(cx, cy - r * 0.5f, cx, cy + r * 0.1f, stroke, glyph);
            canvas.FillCircle(cx, cy + r * 0.45f, stroke * 1.2f, glyph);
        }
        else // "i"
        {
            canvas.FillCircle(cx, cy - r * 0.45f, stroke * 1.2f, glyph);
            canvas.FillSegment(cx, cy - r * 0.1f, cx, cy + r * 0.5f, stroke, glyph);
        }
    }

    /// <summary>Colors for the static texts, the number fields and the language list.</summary>
    public static nint OnControlColor(uint message, nint dc, nint control)
    {
        if (message is WM_CTLCOLOREDIT or WM_CTLCOLORLISTBOX)
            return SetColors(dc, s_palette.Text, s_palette.Control, s_controlBrush);

        int id = GetDlgCtrlID(control);
        return id switch
        {
            BatteryHeadingId or GeneralHeadingId => SetColors(dc, s_palette.Text, s_palette.Background, s_backgroundBrush),
            InfoTextId => SetColors(dc, s_palette.Text, InfoBarColor, s_infoBrush),
            LowBatteryRangeId or PollRangeId =>
                SetColors(dc, id == ErrorTextId ? s_palette.Error : s_palette.SecondaryText, s_palette.Card, s_cardBrush),
            LowBatteryUnitId or PollUnitId => SetColors(dc, s_palette.SecondaryText, s_palette.Card, s_cardBrush),
            _ => SetColors(dc, s_palette.Text, s_palette.Card, s_cardBrush),
        };
    }

    static nint SetColors(nint dc, uint text, uint background, nint brush)
    {
        SetTextColor(dc, Canvas.ToColorRef(text));
        SetBkColor(dc, Canvas.ToColorRef(background));
        return brush;
    }

    /// <summary>Draws toggles and buttons entirely, keeping the controls themselves (REQ-013-09, REQ-013-11).</summary>
    public static nint OnCustomDraw(NMCUSTOMDRAW* draw)
    {
        if (draw->dwDrawStage != CDDS_PREPAINT)
            return CDRF_DODEFAULT;

        nint control = draw->hdr.hwndFrom;
        int id = (int)draw->hdr.idFrom;
        RECT bounds = draw->rc;
        uint dpi = GetDpiForWindow(control);
        uint state = draw->uItemState;
        bool showFocus = (state & CDIS_FOCUS) != 0 && (SendMessageW(control, WM_QUERYUISTATE, 0, 0) & UISF_HIDEFOCUS) == 0;

        uint behind = id == CloseInfoId ? InfoBarColor : IsToggle(id) ? s_palette.Card : s_palette.Background;
        if (!Canvas.TryCreate(bounds.Width, bounds.Height, out Canvas canvas))
            return CDRF_DODEFAULT;
        canvas.Clear(behind);

        if (IsToggle(id))
            DrawToggle(ref canvas, dpi, state, showFocus, SendMessageW(control, BM_GETCHECK, 0, 0) == BST_CHECKED);
        else if (id == CloseInfoId)
            DrawCloseButton(ref canvas, dpi, state, showFocus, behind);
        else
            DrawButton(ref canvas, control, dpi, state, showFocus, isAccent: id == SaveId);

        canvas.CopyTo(draw->hdc, bounds.Left, bounds.Top);
        canvas.Dispose();
        return CDRF_SKIPDEFAULT;
    }

    static void DrawToggle(ref Canvas canvas, uint dpi, uint state, bool showFocus, bool isOn)
    {
        float width = Dpi.Scale(TrackWidth, dpi), height = Dpi.Scale(TrackHeight, dpi);
        float left = (canvas.Width - width) / 2f, top = (canvas.Height - height) / 2f;
        float right = left + width, bottom = top + height, radius = height / 2f;
        bool isActive = (state & (CDIS_HOT | CDIS_SELECTED)) != 0;
        float thumb = Dpi.Scale(isActive ? ThumbHotRadius : ThumbRadius, dpi);

        if (isOn)
        {
            canvas.FillRoundedRect(left, top, right, bottom, radius, isActive ? Canvas.Mix(s_palette.Accent, s_palette.Card, 0.12f) : s_palette.Accent);
            canvas.FillCircle(right - radius, top + radius, thumb, s_palette.AccentText);
        }
        else
        {
            if (isActive)
                canvas.FillRoundedRect(left, top, right, bottom, radius, Canvas.Mix(s_palette.Card, s_palette.Text, 0.06f));
            canvas.StrokeRoundedRect(left, top, right, bottom, radius, Math.Max(1f, dpi / 96f), s_palette.SecondaryText);
            canvas.FillCircle(left + radius, top + radius, thumb, s_palette.SecondaryText);
        }

        if (showFocus)
        {
            float gap = Dpi.Scale(FocusRingGap, dpi);
            canvas.StrokeRoundedRect(left - gap, top - gap, right + gap, bottom + gap, radius + gap, Dpi.Scale(FocusRingWidth, dpi), s_palette.Text);
        }
    }

    static void DrawButton(ref Canvas canvas, nint control, uint dpi, uint state, bool showFocus, bool isAccent)
    {
        float radius = Dpi.Scale(CornerRadius, dpi);
        bool isDisabled = (state & CDIS_DISABLED) != 0;
        bool isPressed = (state & CDIS_SELECTED) != 0, isHot = (state & CDIS_HOT) != 0;
        uint fill, text;
        if (isAccent && !isDisabled)
        {
            fill = Canvas.Mix(s_palette.Accent, s_palette.Background, isPressed ? 0.2f : isHot ? 0.1f : 0f);
            text = s_palette.AccentText;
        }
        else
        {
            fill = Canvas.Mix(s_palette.Card, s_palette.Text, isPressed ? 0.08f : isHot ? 0.04f : 0f);
            text = isDisabled ? s_palette.SecondaryText : s_palette.Text;
        }

        canvas.FillRoundedRect(0, 0, canvas.Width, canvas.Height, radius, fill);
        if (!isAccent || isDisabled)
            canvas.StrokeRoundedRect(0, 0, canvas.Width, canvas.Height, radius, 1f, s_palette.ControlBorder);
        if (showFocus)
            canvas.StrokeRoundedRect(0, 0, canvas.Width, canvas.Height, radius, Dpi.Scale(FocusRingWidth, dpi), s_palette.Text);

        char* buffer = stackalloc char[TextCapacity];
        int length = GetWindowTextW(control, buffer, TextCapacity);
        nint font = SendMessageW(control, WM_GETFONT, 0, 0);
        nint previousFont = font != 0 ? SelectObject(canvas.Dc, font) : 0;
        SetBkMode(canvas.Dc, TRANSPARENT);
        SetTextColor(canvas.Dc, Canvas.ToColorRef(text));
        var bounds = new RECT { Right = canvas.Width, Bottom = canvas.Height };
        DrawTextW(canvas.Dc, buffer, length, &bounds, DT_CENTER | DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX);
        if (previousFont != 0)
            SelectObject(canvas.Dc, previousFont);
    }

    /// <summary>The info bar's ✕, drawn as two strokes so it needs no symbol font.</summary>
    static void DrawCloseButton(ref Canvas canvas, uint dpi, uint state, bool showFocus, uint behind)
    {
        float radius = Dpi.Scale(CornerRadius, dpi);
        if ((state & (CDIS_HOT | CDIS_SELECTED)) != 0)
            canvas.FillRoundedRect(0, 0, canvas.Width, canvas.Height, radius, Canvas.Mix(behind, s_palette.Text, (state & CDIS_SELECTED) != 0 ? 0.1f : 0.06f));
        if (showFocus)
            canvas.StrokeRoundedRect(0, 0, canvas.Width, canvas.Height, radius, Dpi.Scale(FocusRingWidth, dpi), s_palette.Text);

        float half = Dpi.Scale(5, dpi), stroke = Math.Max(0.6f, dpi / 96f * 0.6f);
        float cx = canvas.Width / 2f, cy = canvas.Height / 2f;
        canvas.FillSegment(cx - half, cy - half, cx + half, cy + half, stroke, s_palette.Text);
        canvas.FillSegment(cx - half, cy + half, cx + half, cy - half, stroke, s_palette.Text);
    }

    static void DeleteIfCreated(ref nint gdiObject)
    {
        if (gdiObject != 0)
            DeleteObject(gdiObject);
        gdiObject = 0;
    }
}
