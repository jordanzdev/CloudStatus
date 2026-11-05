namespace CloudStatus.Core;

internal enum LatestRelease
{
    /// <summary>The redirect points to a tag with a valid version.</summary>
    Found,
    /// <summary>The redirect points to the releases list: nothing has been published yet.</summary>
    NoneYet,
    /// <summary>Anything else.</summary>
    Unknown,
}

internal enum UpdateOutcome
{
    UpToDate,
    /// <summary>Ask, then download, verify and run the MSI.</summary>
    InstallUpdate,
    /// <summary>Ask, then open the release page: the MSI does not manage a portable copy.</summary>
    OpenReleasePage,
}

/// <summary>
/// The pure part of "Check for updates" (spec 010): the only URLs the app ever requests, reading the
/// latest version from GitHub's redirect, finding a hash in SHA256SUMS.txt and choosing what to do.
/// </summary>
internal static class ReleaseInfo
{
    /// <summary>The only host the app ever connects to, always over HTTPS on port 443.</summary>
    public const string Host = "github.com";
    public const string ReleasesPath = "/jordanzdev/CloudStatus/releases";
    public const string LatestPath = ReleasesPath + "/latest";
    public const string LatestUrl = "https://" + Host + LatestPath;
    public const string ChecksumsFileName = "SHA256SUMS.txt";

    const string TagPathMarker = "/releases/tag/v";
    const string ReleasesSuffix = "/releases";

    public static string InstallerFileName(Version version) => $"CloudStatus-{AppVersion.ToText(version)}.msi";

    /// <summary>Path on <see cref="Host"/> of a file attached to the release of <paramref name="version"/>.</summary>
    public static string AssetPath(Version version, string fileName) =>
        $"{ReleasesPath}/download/v{AppVersion.ToText(version)}/{fileName}";

    /// <summary>
    /// Reads the version from the Location header of releases/latest: ".../releases/tag/v1.2.3".
    /// Before the first release GitHub redirects to ".../releases" instead.
    /// </summary>
    public static LatestRelease ParseLatestLocation(string? location, out Version version)
    {
        version = new Version(0, 0, 0);
        if (location == null)
            return LatestRelease.Unknown;

        int tag = location.LastIndexOf(TagPathMarker, StringComparison.Ordinal);
        if (tag >= 0)
        {
            string text = location[(tag + TagPathMarker.Length)..];
            if (!Version.TryParse(text, out Version? parsed) || parsed.Revision >= 0)
                return LatestRelease.Unknown;
            version = AppVersion.Normalize(parsed);
            return LatestRelease.Found;
        }

        return location.TrimEnd('/').EndsWith(ReleasesSuffix, StringComparison.Ordinal) ? LatestRelease.NoneYet : LatestRelease.Unknown;
    }

    public static UpdateOutcome Decide(Version current, Version latest, bool isInstalledCopy)
    {
        if (AppVersion.Normalize(latest) <= AppVersion.Normalize(current))
            return UpdateOutcome.UpToDate;
        return isInstalledCopy ? UpdateOutcome.InstallUpdate : UpdateOutcome.OpenReleasePage;
    }

    /// <summary>Finds the hash of <paramref name="fileName"/> in "hash  name" or "hash *name" lines, as sha256sum writes them.</summary>
    public static string? FindChecksum(string checksums, string fileName)
    {
        foreach (string line in checksums.Split('\n'))
        {
            string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[1].TrimStart('*') == fileName)
                return parts[0];
        }
        return null;
    }
}
