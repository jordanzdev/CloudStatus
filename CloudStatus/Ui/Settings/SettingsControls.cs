using CloudStatus.Native;
using static CloudStatus.Native.Comctl32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui.Settings;

/// <summary>Child controls of the settings window: ids, creation, DPI-aware layout and small accessors.</summary>
internal static unsafe class SettingsControls
{
    public const int ClientWidth = 372;
    public const int ClientHeight = 214;

    public const int LowBatteryNoticeId = 100;
    public const int LowBatteryPercentId = 101;
    public const int PollMinutesId = 102;
    public const int LanguageId = 103;
    public const int StartWithWindowsId = 104;
    public const int CheckForUpdatesId = 105;
    public const int SaveId = IDOK;
    public const int CancelId = IDCANCEL;

    const int LowBatteryLabelId = 200;
    const int LowBatteryRangeLabelId = 201;
    const int PollLabelId = 202;
    const int PollRangeLabelId = 203;
    const int LanguageLabelId = 204;

    const int NumberMaxDigits = 2;

    // id, x, y, width, height at 96 DPI. Combo box heights include the drop-down list.
    static ReadOnlySpan<short> Layout =>
    [
        LowBatteryNoticeId, 12, 12, 348, 20,
        LowBatteryLabelId, 12, 43, 190, 20,
        LowBatteryPercentId, 206, 40, 48, 23,
        LowBatteryRangeLabelId, 262, 43, 98, 20,
        PollLabelId, 12, 75, 190, 20,
        PollMinutesId, 206, 72, 48, 23,
        PollRangeLabelId, 262, 75, 98, 20,
        LanguageLabelId, 12, 107, 190, 20,
        LanguageId, 206, 104, 154, 200,
        StartWithWindowsId, 12, 140, 348, 20,
        CheckForUpdatesId, 12, 176, 160, 26,
        SaveId, 192, 176, 80, 26,
        CancelId, 280, 176, 80, 26,
    ];

    /// <summary>Creates every control. Creation order is the Tab order.</summary>
    public static void Create(nint window)
    {
        CreateCheckBox(window, LowBatteryNoticeId, Strings.CheckLowBatteryNotice);
        CreateLabel(window, LowBatteryLabelId, Strings.LabelLowBatteryPercent);
        CreateNumber(window, LowBatteryPercentId);
        CreateLabel(window, LowBatteryRangeLabelId, Strings.LowBatteryRange);
        CreateLabel(window, PollLabelId, Strings.LabelPollMinutes);
        CreateNumber(window, PollMinutesId);
        CreateLabel(window, PollRangeLabelId, Strings.PollRange);
        CreateLabel(window, LanguageLabelId, Strings.LabelLanguage);
        CreateChild(window, LanguageId, "COMBOBOX", null, WS_TABSTOP | WS_VSCROLL | CBS_DROPDOWNLIST, 0);
        CreateCheckBox(window, StartWithWindowsId, Strings.CheckStartWithWindows);
        CreateChild(window, CheckForUpdatesId, "BUTTON", Strings.ButtonCheckForUpdates, WS_TABSTOP | BS_PUSHBUTTON, 0);
        CreateChild(window, SaveId, "BUTTON", Strings.ButtonSave, WS_TABSTOP | BS_DEFPUSHBUTTON, 0);
        CreateChild(window, CancelId, "BUTTON", Strings.ButtonCancel, WS_TABSTOP | BS_PUSHBUTTON, 0);
    }

    /// <summary>Applies <paramref name="font"/> and positions scaled to <paramref name="dpi"/>.</summary>
    public static void ApplyLayout(nint window, uint dpi, nint font)
    {
        ReadOnlySpan<short> layout = Layout;
        for (int i = 0; i + 4 < layout.Length; i += 5)
        {
            nint control = Get(window, layout[i]);
            if (font != 0)
                SendMessageW(control, WM_SETFONT, font, 1);
            SetWindowPos(control, 0,
                Dpi.Scale(layout[i + 1], dpi), Dpi.Scale(layout[i + 2], dpi),
                Dpi.Scale(layout[i + 3], dpi), Dpi.Scale(layout[i + 4], dpi),
                SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    public static nint Get(nint window, int id) => GetDlgItem(window, id);

    public static void Focus(nint window, int id) => SetFocus(Get(window, id));

    /// <summary>Selects the field and shows <paramref name="text"/> in a balloon next to it.</summary>
    public static void ReportInvalid(nint window, int id, string text)
    {
        nint control = Get(window, id);
        SetFocus(control);
        SendMessageW(control, EM_SETSEL, 0, -1);
        fixed (char* title = Strings.AppName, chars = text)
        {
            var tip = new EDITBALLOONTIP
            {
                cbStruct = (uint)sizeof(EDITBALLOONTIP),
                pszTitle = title,
                pszText = chars,
                ttiIcon = TTI_WARNING,
            };
            SendMessageW(control, EM_SHOWBALLOONTIP, 0, (nint)(&tip));
        }
    }

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

    static void CreateLabel(nint window, int id, string text) => CreateChild(window, id, "STATIC", text, SS_LEFT, 0);

    static void CreateCheckBox(nint window, int id, string text) =>
        CreateChild(window, id, "BUTTON", text, WS_TABSTOP | BS_AUTOCHECKBOX, 0);

    static void CreateNumber(nint window, int id)
    {
        CreateChild(window, id, "EDIT", null, WS_TABSTOP | ES_NUMBER | ES_AUTOHSCROLL, WS_EX_CLIENTEDGE);
        SendMessageW(Get(window, id), EM_SETLIMITTEXT, NumberMaxDigits, 0);
    }

    static void CreateChild(nint window, int id, string className, string? text, uint style, uint exStyle) =>
        CreateWindowExW(exStyle, className, text, WS_CHILD | WS_VISIBLE | style, 0, 0, 0, 0, window, id, AppInstance.Handle, 0);
}
