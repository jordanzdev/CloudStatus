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
/// </summary>
internal static class UpdateCheck
{
    const int OpenSucceededAbove = 32; // ShellExecuteW returns a value greater than 32 on success

    public static void Run(nint owner, Action<string> reportProgress)
    {
        reportProgress(Strings.CheckingForUpdates);
        if (!GitHubReleases.TryGetLatest(out LatestRelease result, out Version latest) || result == LatestRelease.Unknown)
        {
            Show(owner, Strings.UpdateCheckFailed, MB_ICONWARNING);
            return;
        }
        if (result == LatestRelease.NoneYet)
        {
            Show(owner, Strings.NoReleaseYet, MB_ICONINFORMATION);
            return;
        }

        string current = AppVersion.CurrentText, available = AppVersion.ToText(latest);
        switch (ReleaseInfo.Decide(AppVersion.Current, latest, InstallRegistration.IsRunningInstalledCopy))
        {
            case UpdateOutcome.UpToDate:
                Show(owner, Strings.UpToDate(current), MB_ICONINFORMATION);
                return;

            case UpdateOutcome.OpenReleasePage:
                // A portable copy is not managed by the MSI: let the user download it.
                if (Ask(owner, Strings.UpdateAvailablePortable(available, current)) && !Open(owner, ReleaseInfo.LatestUrl))
                    Show(owner, Strings.ReleasePageFailed, MB_ICONWARNING);
                return;
        }

        if (!Ask(owner, Strings.UpdateAvailable(available, current)))
            return;

        reportProgress(Strings.DownloadingUpdate);
        switch (UpdateDownloader.TryDownloadInstaller(latest, out string installerPath))
        {
            case DownloadResult.NetworkError:
                Show(owner, Strings.UpdateDownloadFailed, MB_ICONWARNING);
                return;
            case DownloadResult.IntegrityError:
                Show(owner, Strings.UpdateIntegrityFailed, MB_ICONWARNING);
                return;
        }

        if (!Open(owner, installerPath))
        {
            Show(owner, Strings.UpdateLaunchFailed, MB_ICONWARNING);
            return;
        }

        // The installer replaces the exe and offers to reopen CloudStatus on its last page.
        App.Exit();
    }

    static bool Open(nint owner, string target) =>
        ShellExecuteW(owner, "open", target, null, null, SW_SHOWNORMAL) > OpenSucceededAbove;

    static bool Ask(nint owner, string question) =>
        MessageBoxW(owner, question, Strings.AppName, MB_YESNO | MB_ICONQUESTION) == IDYES;

    static void Show(nint owner, string text, uint icon) =>
        MessageBoxW(owner, text, Strings.AppName, MB_OK | icon);
}
