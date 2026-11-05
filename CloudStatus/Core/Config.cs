namespace CloudStatus.Core;

internal enum UiLanguage : uint
{
    /// <summary>Spanish when the Windows display language is Spanish, English otherwise.</summary>
    Auto = 0,
    Spanish = 1,
    English = 2,
}

/// <summary>
/// User settings (spec 007). Stored in HKCU\Software\CloudStatus; <see cref="FromStored"/> turns the
/// raw values into a valid configuration, value by value.
/// </summary>
internal record struct Config(bool LowBatteryNotice, byte LowBatteryPercent, uint PollMinutes, UiLanguage Language)
{
    public const byte MinLowBatteryPercent = 15;
    public const byte MaxLowBatteryPercent = 45;
    public const uint MinPollMinutes = 1;
    public const uint MaxPollMinutes = 30;

    public static readonly Config Default = new(
        LowBatteryNotice: true,
        LowBatteryPercent: IconDescription.DefaultLowLimit,
        PollMinutes: 5,
        Language: UiLanguage.Auto);

    /// <summary>The settings in use. Replaced as a whole when saved.</summary>
    public static Config Current = Default;

    public readonly uint PollIntervalMs => PollMinutes * 60 * 1000;

    public static bool IsValidLowBatteryPercent(uint value) => value is >= MinLowBatteryPercent and <= MaxLowBatteryPercent;

    public static bool IsValidPollMinutes(uint value) => value is >= MinPollMinutes and <= MaxPollMinutes;

    /// <summary>
    /// Builds the settings from stored DWORDs. A missing (null) or invalid value falls back to its
    /// default without affecting the others.
    /// </summary>
    public static Config FromStored(uint? lowBatteryNotice, uint? lowBatteryPercent, uint? pollMinutes, uint? language)
    {
        Config config = Default;
        if (lowBatteryNotice is 0 or 1)
            config.LowBatteryNotice = lowBatteryNotice == 1;
        if (lowBatteryPercent is uint percent && IsValidLowBatteryPercent(percent))
            config.LowBatteryPercent = (byte)percent;
        if (pollMinutes is uint minutes && IsValidPollMinutes(minutes))
            config.PollMinutes = minutes;
        if (language is uint value && value <= (uint)UiLanguage.English)
            config.Language = (UiLanguage)value;
        return config;
    }
}
