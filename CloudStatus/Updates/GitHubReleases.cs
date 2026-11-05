using CloudStatus.Core;

namespace CloudStatus.Updates;

/// <summary>
/// Release information from github.com (spec 010). The latest version is read from the redirect of
/// releases/latest, which is not followed: no API calls, no JSON and no rate limits. The URLs come
/// from <see cref="ReleaseInfo"/>.
/// </summary>
internal static class GitHubReleases
{
    const int MaxDownloadBytes = 32 * 1024 * 1024;

    /// <summary>False only when the request itself failed; otherwise the redirect says what was found.</summary>
    public static bool TryGetLatest(out LatestRelease result, out Version latest)
    {
        result = LatestRelease.Unknown;
        latest = new Version(0, 0, 0);
        bool sent = HttpsRequest.TrySend(ReleaseInfo.LatestPath, followRedirects: false, out HttpsRequest response);
        using (response)
        {
            if (!sent || !response.IsRedirect)
                return false;
            result = ReleaseInfo.ParseLatestLocation(response.GetLocationHeader(), out latest);
            return true;
        }
    }

    /// <summary>Downloads a file attached to the release of <paramref name="version"/>.</summary>
    public static bool TryDownloadAsset(Version version, string fileName, out byte[] content)
    {
        content = [];
        bool sent = HttpsRequest.TrySend(ReleaseInfo.AssetPath(version, fileName), followRedirects: true, out HttpsRequest response);
        using (response)
            return sent && response.IsSuccess && response.TryReadBody(MaxDownloadBytes, out content);
    }
}
