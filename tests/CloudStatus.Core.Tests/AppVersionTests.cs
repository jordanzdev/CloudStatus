using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3.4", "1.2.3")]
    [InlineData("1.2", "1.2.0")]
    public void Normalize_KeepsMajorMinorPatch(string input, string expected)
    {
        Assert.Equal(expected, AppVersion.ToText(AppVersion.Normalize(new Version(input))));
    }

    [Fact]
    public void Normalize_TreatsMissingVersionAsZero()
    {
        Assert.Equal(new Version(0, 0, 0), AppVersion.Normalize(null));
    }

    [Fact]
    public void Normalize_MakesThreeAndFourPartVersionsEqual()
    {
        Assert.Equal(AppVersion.Normalize(new Version(1, 2, 3)), AppVersion.Normalize(new Version(1, 2, 3, 0)));
    }
}
