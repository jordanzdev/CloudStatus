using CloudStatus.Native;

namespace CloudStatus.Ui;

/// <summary>User-facing text in Spanish and English, chosen from the Windows display language.</summary>
internal static class Strings
{
    public const string AppName = "CloudStatus";

    static bool s_isEnglish;

    public static void Initialize() => s_isEnglish = !IsWindowsInSpanish();

    static bool IsWindowsInSpanish() =>
        (Kernel32.GetUserDefaultUILanguage() & Kernel32.PRIMARY_LANGUAGE_MASK) == Kernel32.LANG_SPANISH;

    static string Pick(string spanish, string english) => s_isEnglish ? english : spanish;

    // Tray
    public static string TooltipDongleConnected => Pick("CloudStatus: Dongle conectado", "CloudStatus: Dongle connected");
    public static string TooltipNoDongle => Pick("CloudStatus: Sin dongle", "CloudStatus: No dongle");
    public static string MenuExit => Pick("Salir", "Exit");
}
