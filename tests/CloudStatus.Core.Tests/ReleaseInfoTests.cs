using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

/// <summary>Spec 010, AC-010-01 to AC-010-04.</summary>
public class ReleaseInfoTests
{
    const string Releases = "https://github.com/jordanzdev/CloudStatus/releases";

    [Theory]
    [InlineData(Releases + "/tag/v1.2.3", "1.2.3")]
    [InlineData(Releases + "/tag/v1.2", "1.2.0")]
    public void Location_WithTag_GivesTheVersion(string location, string expected) // AC-010-01
    {
        Assert.Equal(LatestRelease.Found, ReleaseInfo.ParseLatestLocation(location, out Version version));
        Assert.Equal(Version.Parse(expected), version);
    }

    [Theory]
    [InlineData(Releases + "/tag/1.2.3")]
    [InlineData(Releases + "/tag/vX")]
    [InlineData(Releases + "/tag/v1.2.3.4")]
    [InlineData("https://github.com/login")]
    [InlineData("")]
    [InlineData(null)]
    public void Location_WithoutAVersion_IsUnknown(string? location) // AC-010-01
    {
        Assert.Equal(LatestRelease.Unknown, ReleaseInfo.ParseLatestLocation(location, out _));
    }

    [Theory]
    [InlineData(Releases)]
    [InlineData(Releases + "/")]
    public void Location_OfTheReleasesList_MeansNoReleaseYet(string location) // AC-010-01, REQ-010-06
    {
        Assert.Equal(LatestRelease.NoneYet, ReleaseInfo.ParseLatestLocation(location, out _));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true, UpdateOutcome.InstallUpdate)]
    [InlineData("1.0.0", "1.1.0", false, UpdateOutcome.OpenReleasePage)]
    [InlineData("1.0.0", "1.0.0", true, UpdateOutcome.UpToDate)]
    [InlineData("1.0.0", "1.0.0", false, UpdateOutcome.UpToDate)]
    [InlineData("1.2.0", "1.1.9", true, UpdateOutcome.UpToDate)]
    [InlineData("1.0.0.0", "1.0.0", true, UpdateOutcome.UpToDate)]
    internal void Outcome_FollowsTheTable(string current, string latest, bool isInstalled, UpdateOutcome expected) // AC-010-02
    {
        Assert.Equal(expected, ReleaseInfo.Decide(Version.Parse(current), Version.Parse(latest), isInstalled));
    }

    [Fact]
    public void EveryRequest_IsGitHubReleasesOfThisRepository() // AC-010-03
    {
        var version = new Version(1, 2, 3);
        string[] paths =
        [
            ReleaseInfo.LatestPath,
            ReleaseInfo.AssetPath(version, ReleaseInfo.InstallerFileName(version)),
            ReleaseInfo.AssetPath(version, ReleaseInfo.ChecksumsFileName),
        ];

        Assert.Equal("github.com", ReleaseInfo.Host); // HttpsRequest connects only to this host, over HTTPS
        foreach (string path in paths)
        {
            var url = new Uri("https://" + ReleaseInfo.Host + path);
            Assert.StartsWith(Releases + "/", url.AbsoluteUri);
            Assert.Equal(path, url.PathAndQuery); // nothing in the path can change the host
        }
        Assert.Equal(Releases + "/latest", ReleaseInfo.LatestUrl);
        Assert.EndsWith("/download/v1.2.3/CloudStatus-1.2.3.msi", paths[1]);
    }

    const string HashA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    const string HashB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(HashA + "  CloudStatus-1.2.3.msi\n" + HashB + "  CloudStatus-1.2.3-portable.exe\n")]
    [InlineData(HashB + "  CloudStatus-1.2.3-portable.exe\r\n" + HashA + "  CloudStatus-1.2.3.msi\r\n")]
    [InlineData(HashA + " *CloudStatus-1.2.3.msi\n")]
    public void Checksum_FindsTheExactName(string checksums) // AC-010-04
    {
        Assert.Equal(HashA, ReleaseInfo.FindChecksum(checksums, "CloudStatus-1.2.3.msi"));
    }

    [Theory]
    [InlineData(HashA + "  CloudStatus-1.2.3.msi.bak\n")]
    [InlineData(HashA + "  Old-CloudStatus-1.2.3.msi\n")]
    [InlineData(HashA + "  CloudStatus-1.2.4.msi\n")]
    [InlineData("")]
    public void Checksum_IgnoresOtherNames(string checksums) // AC-010-04
    {
        Assert.Null(ReleaseInfo.FindChecksum(checksums, "CloudStatus-1.2.3.msi"));
    }
}
