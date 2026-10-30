using CloudStatus.Core;
using Xunit;

namespace CloudStatus.Core.Tests;

public class DevicePathTests
{
    const string Path = @"\\?\HID#VID_03F0&PID_018B&MI_03&COL03#8&2e705f35&0&0002#{4d1e55b2-f16f-11cf-88cb-001111000030}";

    [Theory]
    [InlineData(Path)]
    [InlineData(@"\\?\hid#vid_03f0&pid_018b&mi_03&col03#8&2e705f35&0&0002#{4d1e55b2-f16f-11cf-88cb-001111000030}")]
    public void TryParseIds_IgnoresCase(string path)
    {
        Assert.True(DevicePath.TryParseIds(path, out ushort vendorId, out ushort productId));
        Assert.Equal(0x03F0, vendorId);
        Assert.Equal(0x018B, productId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a path")]
    [InlineData(@"\\?\HID#VID_03F0")]
    [InlineData(@"\\?\HID#PID_018B")]
    [InlineData(@"\\?\HID#VID_03F")]
    [InlineData(@"\\?\HID#VID_03F0&PID_01")]
    [InlineData(@"\\?\HID#VID_ZZZZ&PID_018B")]
    [InlineData(@"\\?\HID#VID_+3F0&PID_018B")]
    [InlineData(@"\\?\HID#VID_03F0&PID_ 18B")]
    public void TryParseIds_RejectsMalformedPaths(string path)
    {
        Assert.False(DevicePath.TryParseIds(path, out _, out _));
    }

    [Fact]
    public void IsSame_IgnoresCase()
    {
        Assert.True(DevicePath.IsSame(Path, Path.ToLowerInvariant()));
    }

    [Fact]
    public void IsSame_TellsCollectionsApart()
    {
        Assert.False(DevicePath.IsSame(Path, Path.Replace("COL03", "COL04")));
    }
}
