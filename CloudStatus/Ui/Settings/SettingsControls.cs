using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui.Settings;

internal enum RowKind : byte
{
    Toggle,
    Number,
    Combo,
}

/// <summary>One settings row: a card with a label on the left and a control on the right (REQ-013-08).</summary>
internal readonly record struct RowSpec(RowKind Kind, int LabelId, int ControlId, int SecondaryId, int UnitId, int Height);

/// <summary>
/// Child controls of the settings window: ids, creation, and a DPI-aware layout of section headings
/// and row cards (spec 013). Every text is a STATIC control, so screen readers name the fields; the
/// window only paints the shapes behind them.
/// </summary>
internal static unsafe class SettingsControls
{
    public const int ClientWidth = 440; // at 96 DPI

    public const int LowBatteryNoticeId = 100;
    public const int LowBatteryPercentId = 101;
    public const int PollMinutesId = 102;
    public const int LanguageId = 103;
    public const int StartWithWindowsId = 104;
    public const int CheckForUpdatesId = 105;
    public const int CloseInfoId = 106;
    public const int SaveId = IDOK;
    public const int CancelId = IDCANCEL;

    public const int LowBatteryNoticeLabelId = 200;
    public const int LowBatteryLabelId = 201;
    public const int PollLabelId = 202;
    public const int LanguageLabelId = 203;
    public const int StartLabelId = 204;
    public const int LowBatteryRangeId = 210;
    public const int PollRangeId = 211;
    public const int LowBatteryUnitId = 220;
    public const int PollUnitId = 221;
    public const int BatteryHeadingId = 300;
    public const int GeneralHeadingId = 301;
    public const int InfoTextId = 302;

    public const int RowCount = 5;
    const int BatteryRowCount = 2; // the rest are General

    // At 96 DPI.
    const int Margin = 16;
    const int RowPadding = 16;
    const int RowGap = 4;
    const int GroupGap = 16;
    const int HeadingHeight = 20;
    const int HeadingAdvance = 28;
    const int LineHeight = 20;
    const int ToggleWidth = 48, ToggleHeight = 28;
    const int NumberBoxWidth = 64, NumberBoxHeight = 32, UnitWidth = 28;
    const int ComboWidth = 200, ComboListHeight = 200;
    const int ButtonHeight = 32, CheckButtonWidth = 184, DialogButtonWidth = 96, ButtonGap = 8;
    const int InfoTextLeft = 44, InfoTextTop = 14, InfoCloseSize = 32, InfoCloseMargin = 8, InfoMinHeight = 48, InfoGap = 12;
    const int NumberMaxDigits = 2;

    static readonly RowSpec[] Rows =
    [
        new(RowKind.Toggle, LowBatteryNoticeLabelId, LowBatteryNoticeId, 0, 0, 52),
        new(RowKind.Number, LowBatteryLabelId, LowBatteryPercentId, LowBatteryRangeId, LowBatteryUnitId, 64),
        new(RowKind.Number, PollLabelId, PollMinutesId, PollRangeId, PollUnitId, 64),
        new(RowKind.Combo, LanguageLabelId, LanguageId, 0, 0, 52),
        new(RowKind.Toggle, StartLabelId, StartWithWindowsId, 0, 0, 52),
    ];

    static readonly RECT[] s_cards = new RECT[RowCount];
    static readonly RECT[] s_numberBoxes = new RECT[RowCount];

    /// <summary>Pixel rectangles from the last layout, for painting.</summary>
    public static ReadOnlySpan<RECT> Cards => s_cards;
    public static ReadOnlySpan<RECT> NumberBoxes => s_numberBoxes; // empty for rows without a number
    public static RowSpec Row(int index) => Rows[index];
    public static RECT InfoBar { get; private set; }
    public static bool HasInfoBar { get; private set; }
    public static int ClientHeight { get; private set; }

    /// <summary>Creates every control. Creation order is the Tab order, and each label precedes its field.</summary>
    public static void Create(nint window)
    {
        CreateChild(window, CloseInfoId, "BUTTON", Strings.ButtonCloseMessage, WS_TABSTOP | BS_PUSHBUTTON, visible: false);
        CreateLabel(window, InfoTextId, null, visible: false);

        CreateLabel(window, BatteryHeadingId, Strings.HeadingBattery);
        CreateLabel(window, LowBatteryNoticeLabelId, Strings.LabelLowBatteryNotice);
        CreateToggle(window, LowBatteryNoticeId, Strings.LabelLowBatteryNotice);
        CreateLabel(window, LowBatteryLabelId, Strings.LabelLowBatteryPercent);
        CreateNumber(window, LowBatteryPercentId);
        CreateLabel(window, LowBatteryUnitId, Strings.UnitPercent);
        CreateLabel(window, LowBatteryRangeId, Strings.LowBatteryRange);

        CreateLabel(window, GeneralHeadingId, Strings.HeadingGeneral);
        CreateLabel(window, PollLabelId, Strings.LabelPollMinutes);
        CreateNumber(window, PollMinutesId);
        CreateLabel(window, PollUnitId, Strings.UnitMinutes);
        CreateLabel(window, PollRangeId, Strings.PollRange);
        CreateLabel(window, LanguageLabelId, Strings.LabelLanguage);
        CreateChild(window, LanguageId, "COMBOBOX", null, WS_TABSTOP | WS_VSCROLL | CBS_DROPDOWNLIST);
        CreateLabel(window, StartLabelId, Strings.LabelStartWithWindows);
        CreateToggle(window, StartWithWindowsId, Strings.LabelStartWithWindows);

        CreateChild(window, CheckForUpdatesId, "BUTTON", Strings.ButtonCheckForUpdates, WS_TABSTOP | BS_PUSHBUTTON);
        CreateChild(window, SaveId, "BUTTON", Strings.ButtonSave, WS_TABSTOP | BS_DEFPUSHBUTTON);
        CreateChild(window, CancelId, "BUTTON", Strings.ButtonCancel, WS_TABSTOP | BS_PUSHBUTTON);
    }

    /// <summary>
    /// Places every control for <paramref name="dpi"/> and sets the fonts. The info bar, when
    /// <paramref name="infoText"/> is not null, sits at the top and pushes the rest down.
    /// </summary>
    public static void ApplyLayout(nint window, uint dpi, nint bodyFont, nint headingFont, string? infoText)
    {
        int S(int value) => Dpi.Scale(value, dpi);
        int left = S(Margin), right = S(Margin + (ClientWidth - 2 * Margin));
        int y = S(Margin);

        HasInfoBar = infoText is not null;
        if (infoText is not null)
        {
            int textWidth = right - left - S(InfoTextLeft) - S(InfoCloseSize + 2 * InfoCloseMargin);
            int textHeight = Math.Max(S(LineHeight), MeasureTextHeight(window, bodyFont, infoText, textWidth));
            int height = Math.Max(S(InfoMinHeight), textHeight + 2 * S(InfoTextTop));
            InfoBar = new RECT { Left = left, Top = y, Right = right, Bottom = y + height };
            Place(window, InfoTextId, left + S(InfoTextLeft), y + S(InfoTextTop), textWidth, textHeight, bodyFont);
            Place(window, CloseInfoId, right - S(InfoCloseMargin + InfoCloseSize), y + S(InfoCloseMargin), S(InfoCloseSize), S(InfoCloseSize), bodyFont);
            y += height + S(InfoGap);
        }

        int lineHeight = MeasureTextHeight(window, bodyFont, "0", int.MaxValue);
        for (int i = 0; i < RowCount; i++)
        {
            if (i == 0 || i == BatteryRowCount)
            {
                if (i != 0)
                    y += S(GroupGap);
                Place(window, i == 0 ? BatteryHeadingId : GeneralHeadingId, left + S(2), y, right - left, S(HeadingHeight), headingFont);
                y += S(HeadingAdvance);
            }

            RowSpec row = Rows[i];
            var card = new RECT { Left = left, Top = y, Right = right, Bottom = y + S(row.Height) };
            s_cards[i] = card;
            s_numberBoxes[i] = default;
            PlaceRow(window, row, card, dpi, bodyFont, lineHeight, ref s_numberBoxes[i]);
            y = card.Bottom + S(RowGap);
        }

        y += S(GroupGap) - S(RowGap) + S(4);
        int buttonRight = right;
        Place(window, CancelId, buttonRight - S(DialogButtonWidth), y, S(DialogButtonWidth), S(ButtonHeight), bodyFont);
        buttonRight -= S(DialogButtonWidth + ButtonGap);
        Place(window, SaveId, buttonRight - S(DialogButtonWidth), y, S(DialogButtonWidth), S(ButtonHeight), bodyFont);
        Place(window, CheckForUpdatesId, left, y, S(CheckButtonWidth), S(ButtonHeight), bodyFont);
        ClientHeight = y + S(ButtonHeight) + S(Margin);
    }

    static void PlaceRow(nint window, in RowSpec row, in RECT card, uint dpi, nint font, int lineHeight, ref RECT numberBox)
    {
        int S(int value) => Dpi.Scale(value, dpi);
        int inner = card.Right - S(RowPadding);
        int middle = (card.Top + card.Bottom) / 2;
        int controlLeft;

        switch (row.Kind)
        {
            case RowKind.Toggle:
                controlLeft = inner - S(ToggleWidth);
                Place(window, row.ControlId, controlLeft, middle - S(ToggleHeight) / 2, S(ToggleWidth), S(ToggleHeight), font);
                break;

            case RowKind.Number:
                int unitLeft = inner - S(UnitWidth);
                controlLeft = unitLeft - S(ButtonGap) - S(NumberBoxWidth);
                numberBox = new RECT { Left = controlLeft, Top = middle - S(NumberBoxHeight) / 2, Right = unitLeft - S(ButtonGap) };
                numberBox.Bottom = numberBox.Top + S(NumberBoxHeight);
                // The edit has no border of its own: the window draws the box around it.
                int editInset = S(6);
                Place(window, row.ControlId, numberBox.Left + editInset, middle - lineHeight / 2,
                    numberBox.Right - numberBox.Left - 2 * editInset, lineHeight, font);
                Place(window, row.UnitId, unitLeft, middle - lineHeight / 2, S(UnitWidth), lineHeight, font);
                break;

            default: // RowKind.Combo: its height comes from the font, so center it once placed
                controlLeft = inner - S(ComboWidth);
                nint combo = Place(window, row.ControlId, controlLeft, card.Top, S(ComboWidth), S(ComboListHeight), font);
                RECT field;
                if (GetWindowRect(combo, &field))
                    SetWindowPos(combo, 0, controlLeft, middle - field.Height / 2, S(ComboWidth), S(ComboListHeight), SWP_NOZORDER | SWP_NOACTIVATE);
                break;
        }

        int labelLeft = card.Left + S(RowPadding);
        int labelWidth = controlLeft - S(12) - labelLeft;
        if (row.SecondaryId == 0)
        {
            Place(window, row.LabelId, labelLeft, middle - lineHeight / 2, labelWidth, lineHeight, font);
            return;
        }
        int textTop = middle - lineHeight - S(1);
        Place(window, row.LabelId, labelLeft, textTop, labelWidth, lineHeight, font);
        Place(window, row.SecondaryId, labelLeft, textTop + lineHeight + S(2), labelWidth, lineHeight, font);
    }

    static nint Place(nint window, int id, int x, int y, int width, int height, nint font)
    {
        nint control = Get(window, id);
        if (font != 0)
            SendMessageW(control, WM_SETFONT, font, 0);
        SetWindowPos(control, 0, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        return control;
    }

    /// <summary>Height of <paramref name="text"/> wrapped at <paramref name="width"/> pixels.</summary>
    static int MeasureTextHeight(nint window, nint font, string text, int width)
    {
        nint dc = GetDC(window);
        if (dc == 0)
            return 0;
        nint previous = font != 0 ? Gdi32.SelectObject(dc, font) : 0;
        var bounds = new RECT { Right = width };
        DrawTextW(dc, text, -1, &bounds, DT_CALCRECT | DT_WORDBREAK | DT_NOPREFIX | DT_LEFT);
        if (previous != 0)
            Gdi32.SelectObject(dc, previous);
        ReleaseDC(window, dc);
        return bounds.Height;
    }

    /// <summary>The row whose secondary text shows <paramref name="controlId"/>'s range or error, or 0.</summary>
    public static int SecondaryIdFor(int controlId)
    {
        foreach (RowSpec row in Rows)
            if (row.ControlId == controlId)
                return row.SecondaryId;
        return 0;
    }

    public static string RangeTextFor(int secondaryId) =>
        secondaryId == LowBatteryRangeId ? Strings.LowBatteryRange : Strings.PollRange;

    public static bool IsToggle(int id) => id is LowBatteryNoticeId or StartWithWindowsId;

    public static bool IsNumber(int id) => id is LowBatteryPercentId or PollMinutesId;

    public static nint Get(nint window, int id) => GetDlgItem(window, id);

    public static void Focus(nint window, int id) => SetFocus(Get(window, id));

    public static bool IsChecked(nint window, int id) =>
        SendMessageW(Get(window, id), BM_GETCHECK, 0, 0) == BST_CHECKED;

    public static void SetChecked(nint window, int id, bool isChecked) =>
        SendMessageW(Get(window, id), BM_SETCHECK, isChecked ? BST_CHECKED : BST_UNCHECKED, 0);

    public static bool TryGetNumber(nint window, int id, out uint value)
    {
        int translated;
        value = GetDlgItemInt(window, id, &translated, false);
        return translated != 0;
    }

    public static void SetNumber(nint window, int id, uint value) => SetDlgItemInt(window, id, value, false);

    public static void AddComboItem(nint window, int id, string text)
    {
        fixed (char* chars = text)
            SendMessageW(Get(window, id), CB_ADDSTRING, 0, (nint)chars);
    }

    public static int GetComboSelection(nint window, int id) => (int)SendMessageW(Get(window, id), CB_GETCURSEL, 0, 0);

    public static void SetComboSelection(nint window, int id, int index) => SendMessageW(Get(window, id), CB_SETCURSEL, index, 0);

    static void CreateLabel(nint window, int id, string? text, bool visible = true) =>
        CreateChild(window, id, "STATIC", text, SS_LEFT | SS_NOPREFIX, visible);

    /// <summary>A real check box, so keyboard and screen readers work; it is drawn as a switch (REQ-013-09).</summary>
    static void CreateToggle(nint window, int id, string label) =>
        CreateChild(window, id, "BUTTON", label, WS_TABSTOP | BS_AUTOCHECKBOX);

    static void CreateNumber(nint window, int id)
    {
        CreateChild(window, id, "EDIT", null, WS_TABSTOP | ES_NUMBER | ES_AUTOHSCROLL | ES_CENTER);
        SendMessageW(Get(window, id), EM_SETLIMITTEXT, NumberMaxDigits, 0);
    }

    static void CreateChild(nint window, int id, string className, string? text, uint style, bool visible = true) =>
        CreateWindowExW(0, className, text, WS_CHILD | (visible ? WS_VISIBLE : 0) | style, 0, 0, 0, 0, window, id, AppInstance.Handle, 0);
}
