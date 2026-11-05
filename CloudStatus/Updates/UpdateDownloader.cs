using System.Text;
using CloudStatus.Core;
using CloudStatus.Native;

namespace CloudStatus.Updates;

internal enum DownloadResult
{
    Ready,
    NetworkError,
    IntegrityError,
}

/// <summary>
/// Downloads the MSI of a release and checks it against the release's SHA256SUMS.txt in memory,
/// before it is ever written to disk (spec 010). It is saved under %TEMP%\CloudStatus-update.
/// </summary>
internal static unsafe class UpdateDownloader
{
    const string DownloadFolderName = "CloudStatus-update";

    public static DownloadResult TryDownloadInstaller(Version version, out string installerPath)
    {
        installerPath = "";
        string fileName = ReleaseInfo.InstallerFileName(version);

        if (!GitHubReleases.TryDownloadAsset(version, ReleaseInfo.ChecksumsFileName, out byte[] checksums) ||
            !GitHubReleases.TryDownloadAsset(version, fileName, out byte[] installer))
            return DownloadResult.NetworkError;

        string? expected = ReleaseInfo.FindChecksum(Encoding.ASCII.GetString(checksums), fileName);
        if (expected == null || !string.Equals(Sha256Hex(installer), expected, StringComparison.OrdinalIgnoreCase))
            return DownloadResult.IntegrityError;

        string folder = Path.Combine(Path.GetTempPath(), DownloadFolderName);
        Directory.CreateDirectory(folder);
        installerPath = Path.Combine(folder, fileName);
        File.WriteAllBytes(installerPath, installer);
        return DownloadResult.Ready;
    }

    static string Sha256Hex(byte[] data)
    {
        byte* hash = stackalloc byte[BCrypt.Sha256Length];
        fixed (byte* input = data)
        {
            if (BCrypt.BCryptHash(BCrypt.BCRYPT_SHA256_ALG_HANDLE, null, 0, input, (uint)data.Length, hash, BCrypt.Sha256Length) != 0)
                return "";
        }
        return Convert.ToHexString(new ReadOnlySpan<byte>(hash, BCrypt.Sha256Length));
    }
}
