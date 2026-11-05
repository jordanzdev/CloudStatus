using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

/// <summary>Spec 007, AC-007-08.</summary>
public class StartupCommandTests
{
    const string Exe = @"C:\Users\me\AppData\Local\Programs\CloudStatus\CloudStatus.exe";

    [Fact]
    public void Build_QuotesThePathAndAddsTheArgument()
    {
        Assert.Equal("\"" + Exe + "\" --startup", StartupCommand.Build(Exe));
    }

    [Fact]
    public void SameCommand_IsCurrent_IgnoringCase()
    {
        Assert.True(StartupCommand.IsCurrent(StartupCommand.Build(Exe), Exe));
        Assert.True(StartupCommand.IsCurrent(StartupCommand.Build(Exe).ToUpperInvariant(), Exe));
    }

    [Fact]
    public void MovedExe_IsNotCurrent()
    {
        Assert.False(StartupCommand.IsCurrent(StartupCommand.Build(@"D:\Tools\CloudStatus.exe"), Exe));
    }

    [Theory]
    [InlineData("\"" + Exe + "\"")]
    [InlineData(Exe + " --startup")]
    [InlineData("\"" + Exe + "\" --startup --other")]
    public void MissingArgumentOrDifferentForm_IsNotCurrent(string registered)
    {
        Assert.False(StartupCommand.IsCurrent(registered, Exe));
    }
}
