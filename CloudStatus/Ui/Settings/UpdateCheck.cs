using CloudStatus.Core;
using CloudStatus.Persistence;
using CloudStatus.Updates;
using static CloudStatus.Native.Shell32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui.Settings;

/// <summary>
/// "Check for updates" (spec 010). This is the only code that uses the network or starts another
/// process, and it runs only when the user clicks the button: never automatically, never in the
/// background. It runs on the UI thread and blocks it for the second or two the requests take (D16).
/// Results that need no answer go to the window's info bar (spec 013); questions stay message boxes.
/// </summary>
internal static class UpdateCheck
{
    const int OpenSucceededAbove = 32; // ShellExecuteW returns a value greater than 32 on success

    /// <param name="showResult">Shows a result in the window: its text, and whether it is a warning.</param>
    public static void Run(nint owner, Action<string> reportProgress, Action<string, bool> showResult)
    {
        reportProgress(Strings.CheckingForUpdates);
        if (!GitHubReleases.TryGetLatest(out LatestRelease result, out Version latest) || result == LatestRelease.Unknown)
        {
            showResult(Strings.UpdateCheckFailed, true);
            return;
        }
        if (result == LatestRelease.NoneYet)
        {
            showResult(Strings.NoReleaseYet, false);
            return;
        }

        string current = AppVersion.CurrentText, available = AppVersion.ToText(latest);
        switch (ReleaseInfo.Decide(AppVersion.Current, latest, InstallRegistration.IsRunningInstalledCopy))
        {
            case UpdateOutcome.UpToDate:
                showResult(Strings.UpToDate(current), false);
                return;

            case UpdateOutcome.OpenReleasePage:
                // A portable copy is not managed by the MSI: let the user download it.
                if (Ask(owner, Strings.UpdateAvailablePortable(available, current)) && !Open(owner, ReleaseInfo.LatestUrl))
                    showResult(Strings.ReleasePageFailed, true);
                return;
        }

        if (!Ask(owner, Strings.UpdateAvailable(available, current)))
            return;

        reportProgress(Strings.DownloadingUpdate);
        switch (UpdateDownloader.TryDownloadInstaller(latest, out string installerPath))
        {
            case DownloadResult.NetworkError:
                showResult(Strings.UpdateDownloadFailed, true);
                return;
            case DownloadResult.IntegrityError:
                showResult(Strings.UpdateIntegrityFailed, true);
                return;
        }

        if (!Open(owner, installerPath))
        {
            showResult(Strings.UpdateLaunchFailed, true);
            return;
        }

        // The installer replaces the exe and offers to reopen CloudStatus on its last page.
        App.Exit();
    }

    static bool Open(nint owner, string target) =>
        ShellExecuteW(owner, "open", target, null, null, SW_SHOWNORMAL) > OpenSucceededAbove;

    static bool Ask(nint owner, string question) =>
        MessageBoxW(owner, question, Strings.AppName, MB_YESNO | MB_ICONQUESTION) == IDYES;
}
