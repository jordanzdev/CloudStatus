using CloudStatus.Core;
using static CloudStatus.Ui.Settings.SettingsControls;

namespace CloudStatus.Ui.Settings;

internal enum SettingsFormError
{
    None,
    InvalidLowBatteryPercent,
    InvalidPollMinutes,
}

/// <summary>Moves values between <see cref="Config"/> and the settings controls, and validates the input.</summary>
internal static class SettingsForm
{
    public static void Populate(nint window, in Config config, bool startWithWindows)
    {
        SetChecked(window, LowBatteryNoticeId, config.LowBatteryNotice);
        SetNumber(window, LowBatteryPercentId, config.LowBatteryPercent);
        SetNumber(window, PollMinutesId, config.PollMinutes);

        // The combo index matches the UiLanguage value.
        AddComboItem(window, LanguageId, Strings.LanguageAuto);
        AddComboItem(window, LanguageId, Strings.LanguageSpanish);
        AddComboItem(window, LanguageId, Strings.LanguageEnglish);
        SetComboSelection(window, LanguageId, (int)config.Language);

        SetChecked(window, StartWithWindowsId, startWithWindows);
    }

    /// <summary>Reads and validates the form. <paramref name="next"/> starts as a copy of <paramref name="current"/>.</summary>
    public static SettingsFormError TryRead(nint window, in Config current, out Config next, out bool startWithWindows)
    {
        next = current;
        startWithWindows = IsChecked(window, StartWithWindowsId);

        if (!TryGetNumber(window, LowBatteryPercentId, out uint percent) || !Config.IsValidLowBatteryPercent(percent))
            return SettingsFormError.InvalidLowBatteryPercent;
        if (!TryGetNumber(window, PollMinutesId, out uint minutes) || !Config.IsValidPollMinutes(minutes))
            return SettingsFormError.InvalidPollMinutes;

        next.LowBatteryNotice = IsChecked(window, LowBatteryNoticeId);
        next.LowBatteryPercent = (byte)percent;
        next.PollMinutes = minutes;
        next.Language = ReadLanguage(window);
        return SettingsFormError.None;
    }

    static UiLanguage ReadLanguage(nint window)
    {
        int index = GetComboSelection(window, LanguageId);
        return index is >= (int)UiLanguage.Auto and <= (int)UiLanguage.English ? (UiLanguage)index : UiLanguage.Auto;
    }
}
