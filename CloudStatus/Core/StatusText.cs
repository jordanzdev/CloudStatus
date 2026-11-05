namespace CloudStatus.Core;

/// <summary>
/// The state in words, for the tooltip and the menu (spec 013, REQ-013-20 and REQ-013-21). Pure, so
/// that both languages can be checked against the tooltip's 128 characters.
/// </summary>
internal static class StatusText
{
    public const string AppName = "CloudStatus";
    public const int TooltipCapacity = 128; // szTip, including the terminating null

    /// <summary>The state line: "78 % · Charging", "Headset off", "No dongle"…</summary>
    public static string Line(bool isEnglish, HeadsetPower power, byte percent, ChargeState charge) => power switch
    {
        HeadsetPower.On => charge switch
        {
            ChargeState.Charging => $"{percent} % · " + (isEnglish ? "Charging" : "Cargando"),
            ChargeState.Full => $"{percent} % · " + (isEnglish ? "Full" : "Completa"),
            ChargeState.Error => $"{percent} % · " + (isEnglish ? "Charging error" : "Error de carga"),
            _ => $"{percent} %",
        },
        HeadsetPower.Off => isEnglish ? "Headset off" : "Cascos apagados",
        HeadsetPower.Checking => isEnglish ? "Dongle connected" : "Dongle conectado",
        _ => isEnglish ? "No dongle" : "Sin dongle",
    };

    /// <summary>Two lines: the app name, then the state.</summary>
    public static string Tooltip(bool isEnglish, HeadsetPower power, byte percent, ChargeState charge) =>
        AppName + "\n" + Line(isEnglish, power, percent, charge);
}
