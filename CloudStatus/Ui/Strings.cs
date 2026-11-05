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

    // Tray (spec 013: two-line tooltip, and the state line at the top of the menu)
    public static string Tooltip(HeadsetPower power, byte percent, ChargeState charge) =>
        StatusText.Tooltip(s_isEnglish, power, percent, charge);
    public static string StatusLine(HeadsetPower power, byte percent, ChargeState charge) =>
        StatusText.Line(s_isEnglish, power, percent, charge);
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
    public static string HeadingBattery => Pick("Batería", "Battery");
    public static string HeadingGeneral => Pick("General", "General");
    public static string LabelLowBatteryNotice => Pick("Avisar cuando la batería esté baja", "Notify when the battery is low");
    public static string LabelLowBatteryPercent => Pick("Batería baja por debajo de", "Low battery below");
    public static string LabelPollMinutes => Pick("Leer la batería cada", "Read the battery every");
    public static string LabelLanguage => Pick("Idioma", "Language");
    public static string LabelStartWithWindows => Pick("Iniciar con Windows", "Start with Windows");
    public const string UnitPercent = "%";
    public const string UnitMinutes = "min";
    public static string ButtonCloseMessage => Pick("Cerrar mensaje", "Close message");
    public static string ButtonSave => Pick("Guardar", "Save");
    public static string ButtonCancel => Pick("Cancelar", "Cancel");
    public static string LanguageAuto => Pick("Automático (sistema)", "Automatic (system)");
    public const string LanguageSpanish = "Español";
    public const string LanguageEnglish = "English";
    public static string LowBatteryRange => s_isEnglish
        ? $"{Config.MinLowBatteryPercent} to {Config.MaxLowBatteryPercent} %"
        : $"Entre {Config.MinLowBatteryPercent} y {Config.MaxLowBatteryPercent} %";
    public static string PollRange => s_isEnglish
        ? $"{Config.MinPollMinutes} to {Config.MaxPollMinutes} minutes"
        : $"Entre {Config.MinPollMinutes} y {Config.MaxPollMinutes} minutos";

    // Shown under the field, in place of the range (spec 013, REQ-013-12).
    public static string ErrorInvalidLowBatteryPercent => s_isEnglish
        ? $"Enter a number from {Config.MinLowBatteryPercent} to {Config.MaxLowBatteryPercent}."
        : $"Escribe un número entre {Config.MinLowBatteryPercent} y {Config.MaxLowBatteryPercent}.";

    public static string ErrorInvalidPollMinutes => s_isEnglish
        ? $"Enter a number from {Config.MinPollMinutes} to {Config.MaxPollMinutes}."
        : $"Escribe un número entre {Config.MinPollMinutes} y {Config.MaxPollMinutes}.";

    public static string ErrorNotSaved => Pick(
        "No se pudieron guardar los ajustes en el registro. Se mantienen los anteriores.",
        "Could not save the settings to the registry. The previous ones are kept.");

    public static string ErrorStartupNotChanged => Pick(
        "Los ajustes se han guardado, pero no se pudo cambiar «Iniciar con Windows».",
        "The settings were saved, but \"Start with Windows\" could not be changed.");

    // Updates (spec 010)
    public static string ButtonCheckForUpdates => Pick("Buscar actualizaciones", "Check for updates");
    public static string CheckingForUpdates => Pick("Comprobando…", "Checking…");
    public static string DownloadingUpdate => Pick("Descargando…", "Downloading…");

    public static string UpdateCheckFailed => Pick(
        "No se pudo comprobar si hay actualizaciones. Revisa tu conexión a Internet.",
        "Could not check for updates. Check your internet connection.");

    public static string NoReleaseYet => Pick(
        "Aún no se ha publicado ninguna versión de CloudStatus.",
        "No version of CloudStatus has been published yet.");

    public static string UpToDate(string current) => s_isEnglish
        ? $"You have the latest version of CloudStatus ({current})."
        : $"Tienes la última versión de CloudStatus ({current}).";

    public static string UpdateAvailable(string available, string current) => s_isEnglish
        ? $"A new version of CloudStatus is available: {available} (you have {current}).\n\nDownload and install it now? CloudStatus will close and the installer will open."
        : $"Hay una versión nueva de CloudStatus: {available} (tienes la {current}).\n\n¿Descargarla e instalarla ahora? CloudStatus se cerrará y se abrirá el instalador.";

    public static string UpdateAvailablePortable(string available, string current) => s_isEnglish
        ? $"A new version of CloudStatus is available: {available} (you have {current}).\n\nYou are using the portable version. Open the download page?"
        : $"Hay una versión nueva de CloudStatus: {available} (tienes la {current}).\n\nEstás usando la versión portable. ¿Abrir la página de descarga?";

    public static string UpdateDownloadFailed => Pick(
        "No se pudo descargar la actualización. Inténtalo de nuevo más tarde.",
        "Could not download the update. Please try again later.");

    public static string UpdateIntegrityFailed => Pick(
        "La descarga no coincide con su suma SHA-256 publicada, así que no se ha instalado. Inténtalo de nuevo más tarde.",
        "The download does not match its published SHA-256 checksum, so it was not installed. Please try again later.");

    public static string UpdateLaunchFailed => Pick(
        "No se pudo abrir el instalador descargado.",
        "Could not open the downloaded installer.");

    public static string ReleasePageFailed => Pick(
        "No se pudo abrir la página de descarga.",
        "Could not open the download page.");

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
