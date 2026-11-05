using CloudStatus.Core;
using CloudStatus.Native;

namespace CloudStatus.Ui;

/// <summary>User-facing text in Spanish and English, in the language chosen in Settings.</summary>
internal static class Strings
{
    public const string AppName = "CloudStatus";

    static bool s_isEnglish;

    public static void Apply(UiLanguage language) =>
        s_isEnglish = language switch
        {
            UiLanguage.Spanish => false,
            UiLanguage.English => true,
            _ => !IsWindowsInSpanish(),
        };

    static bool IsWindowsInSpanish() =>
        (Kernel32.GetUserDefaultUILanguage() & Kernel32.PRIMARY_LANGUAGE_MASK) == Kernel32.LANG_SPANISH;

    static string Pick(string spanish, string english) => s_isEnglish ? english : spanish;

    // Tray
    public static string TooltipDongleConnected => Pick("CloudStatus: Dongle conectado", "CloudStatus: Dongle connected");
    public static string TooltipNoDongle => Pick("CloudStatus: Sin dongle", "CloudStatus: No dongle");
    public static string TooltipHeadsetOff => Pick("CloudStatus: Cascos apagados", "CloudStatus: Headset off");
    public static string TooltipBattery(byte percent, ChargeState charge) => charge switch
    {
        ChargeState.Charging => $"CloudStatus: {percent} %, " + Pick("cargando", "charging"),
        ChargeState.Full => $"CloudStatus: {percent} %, " + Pick("completa", "full"),
        ChargeState.Error => $"CloudStatus: {percent} %, " + Pick("error de carga", "charging error"),
        _ => $"CloudStatus: {percent} %",
    };
    public static string MenuSettings => Pick("Ajustes…", "Settings…");
    public static string MenuExit => Pick("Salir", "Exit");

    // Low-battery notice
    public static string NoticeTitle(BatteryNotice notice) => notice == BatteryNotice.Critical
        ? Pick("Batería muy baja", "Battery very low")
        : Pick("Batería baja", "Low battery");
    public static string NoticeText(BatteryNotice notice, byte percent) => notice == BatteryNotice.Critical
        ? Pick($"Cascos al {percent} %. Cárgalos ya.", $"Headset at {percent} %. Charge it now.")
        : Pick($"Cascos al {percent} %. Cárgalos pronto.", $"Headset at {percent} %. Charge it soon.");

    // Settings window
    public static string SettingsTitle => $"CloudStatus {AppVersion.CurrentText} — " + Pick("Ajustes", "Settings");
    public static string CheckLowBatteryNotice => Pick("Avisar cuando la batería esté baja", "Notify when the battery is low");
    public static string LabelLowBatteryPercent => Pick("Batería baja por debajo de (%):", "Low battery below (%):");
    public static string LabelPollMinutes => Pick("Leer la batería cada (min):", "Read the battery every (min):");
    public static string LabelLanguage => Pick("Idioma:", "Language:");
    public static string CheckStartWithWindows => Pick("Iniciar con Windows", "Start with Windows");
    public static string ButtonSave => Pick("Guardar", "Save");
    public static string ButtonCancel => Pick("Cancelar", "Cancel");
    public static string LanguageAuto => Pick("Automático (sistema)", "Automatic (system)");
    public const string LanguageSpanish = "Español";
    public const string LanguageEnglish = "English";
    public static string LowBatteryRange => $"({Config.MinLowBatteryPercent}–{Config.MaxLowBatteryPercent})";
    public static string PollRange => $"({Config.MinPollMinutes}–{Config.MaxPollMinutes})";

    public static string ErrorInvalidLowBatteryPercent => s_isEnglish
        ? $"The low battery limit must be between {Config.MinLowBatteryPercent} and {Config.MaxLowBatteryPercent} %."
        : $"El límite de batería baja debe estar entre {Config.MinLowBatteryPercent} y {Config.MaxLowBatteryPercent} %.";

    public static string ErrorInvalidPollMinutes => s_isEnglish
        ? $"The battery must be read every {Config.MinPollMinutes} to {Config.MaxPollMinutes} minutes."
        : $"La batería debe leerse cada {Config.MinPollMinutes} a {Config.MaxPollMinutes} minutos.";

    public static string ErrorNotSaved => Pick(
        "No se pudieron guardar los ajustes en el registro. Se mantienen los anteriores.",
        "Could not save the settings to the registry. The previous ones are kept.");

    public static string ErrorStartupNotChanged => Pick(
        "Los ajustes se han guardado, pero no se pudo cambiar «Iniciar con Windows».",
        "The settings were saved, but \"Start with Windows\" could not be changed.");

    // Closing the settings window with the X button
    public static string CloseInstruction => Pick("¿Qué quieres hacer?", "What do you want to do?");
    public static string CloseContent => Pick(
        "Los cambios sin guardar se descartarán.",
        "Unsaved changes will be discarded.");
    public static string CloseMinimizeToTray => Pick(
        "Minimizar a la bandeja\nCloudStatus sigue en segundo plano mostrando la batería.",
        "Minimize to tray\nCloudStatus keeps running in the background showing the battery.");
    public static string CloseExitApplication => Pick(
        "Cerrar CloudStatus\nQuita el icono y termina la aplicación.",
        "Close CloudStatus\nRemoves the icon and exits the application.");
    public static string CloseFallbackQuestion => Pick(
        "¿Minimizar CloudStatus a la bandeja?\n\nSí: sigue en segundo plano mostrando la batería.\nNo: quita el icono y cierra la aplicación.\n\nLos cambios sin guardar se descartarán.",
        "Minimize CloudStatus to the tray?\n\nYes: it keeps running in the background showing the battery.\nNo: removes the icon and exits the application.\n\nUnsaved changes will be discarded.");
}
