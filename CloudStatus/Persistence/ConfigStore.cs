using CloudStatus.Core;

namespace CloudStatus.Persistence;

/// <summary>
/// Loads and saves <see cref="Config"/> under HKCU\Software\CloudStatus (D15). Only called at
/// start-up and on save. Validation is in <see cref="Config.FromStored"/>.
/// </summary>
internal static class ConfigStore
{
    const string KeyPath = @"Software\CloudStatus";

    const string LowBatteryNoticeValue = "LowBatteryNotice";
    const string LowBatteryPercentValue = "LowBatteryPercent";
    const string PollMinutesValue = "PollMinutes";
    const string LanguageValue = "Language";

    public static Config Load()
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForRead(KeyPath);
        if (!key.IsOpen)
            return Config.Default;

        return Config.FromStored(
            ReadDword(key, LowBatteryNoticeValue),
            ReadDword(key, LowBatteryPercentValue),
            ReadDword(key, PollMinutesValue),
            ReadDword(key, LanguageValue));
    }

    public static bool Save(in Config config)
    {
        using RegistryKeyHandle key = RegistryKeyHandle.OpenForWrite(KeyPath);
        if (!key.IsOpen)
            return false;

        bool saved = key.SetDword(LowBatteryNoticeValue, config.LowBatteryNotice ? 1u : 0u);
        saved &= key.SetDword(LowBatteryPercentValue, config.LowBatteryPercent);
        saved &= key.SetDword(PollMinutesValue, config.PollMinutes);
        saved &= key.SetDword(LanguageValue, (uint)config.Language);
        return saved;
    }

    /// <summary>Null when the value is missing or not a DWORD.</summary>
    static uint? ReadDword(in RegistryKeyHandle key, string name) =>
        key.TryGetDword(name, out uint value) ? value : null;
}
