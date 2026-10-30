using CloudStatus.Core;
using CloudStatus.Device;
using Xunit;

namespace CloudStatus.Core.Tests;

public class InterfaceSelectorTests
{
    const ushort Hp = 0x03F0;
    const ushort Cloud2 = 0x018B;

    // The collections of a real 0x03F0:0x018B dongle (docs/protocol.md, section 2).
    static readonly HidCandidate Col01 = new("col01", Hp, Cloud2, 0x000C, 0x0001, 5, 0);
    static readonly HidCandidate Col02 = new("col02", Hp, Cloud2, 0xFFC0, 0x0001, 2, 0);
    static readonly HidCandidate Col03 = new("col03", Hp, Cloud2, 0xFF90, 0x0303, 20, 20);
    static readonly HidCandidate Col04 = new("col04", Hp, Cloud2, 0xFF00, 0x0001, 64, 64);

    [Fact]
    public void Table_HoldsExactlyTheSupportedDevices()
    {
        SupportedDevice[] expected =
        [
            new(Hp, 0x018B, 0xFF90, 0x0303),
            new(Hp, 0x0696, 0xFF90, 0x0303),
        ];
        Assert.Equal(expected, SupportedDevices.All.ToArray());
    }

    [Fact]
    public void IsSupported_RejectsKingston()
    {
        Assert.False(InterfaceSelector.IsSupported(0x0951, 0x1718, SupportedDevices.All));
    }

    [Theory]
    [InlineData(0x018B)]
    [InlineData(0x0696)]
    public void IsSupported_AcceptsHp(int productId)
    {
        Assert.True(InterfaceSelector.IsSupported(Hp, (ushort)productId, SupportedDevices.All));
    }

    [Fact]
    public void TrySelect_PicksTheProtocolCollectionOfARealDongle()
    {
        Assert.True(InterfaceSelector.TrySelect([Col01, Col02, Col03, Col04], SupportedDevices.All, out HidCandidate selected));
        Assert.Equal(Col03, selected);
    }

    [Fact]
    public void TrySelect_NeverPicksTheUnknownCollection()
    {
        Assert.False(InterfaceSelector.TrySelect([Col01, Col02, Col04], SupportedDevices.All, out _));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(20, 0)]
    public void TrySelect_RejectsMissingReportLengths(int input, int output)
    {
        HidCandidate candidate = Col03 with { InputLength = (ushort)input, OutputLength = (ushort)output };
        Assert.False(InterfaceSelector.TrySelect([candidate], SupportedDevices.All, out _));
    }

    [Fact]
    public void TrySelect_IsStableWhateverTheOrder()
    {
        HidCandidate first = Col03 with { Path = "a" }, second = Col03 with { Path = "b" };

        Assert.True(InterfaceSelector.TrySelect([first, second], SupportedDevices.All, out HidCandidate oneWay));
        Assert.True(InterfaceSelector.TrySelect([second, first], SupportedDevices.All, out HidCandidate otherWay));
        Assert.Equal(first, oneWay);
        Assert.Equal(first, otherWay);
    }

    [Fact]
    public void TrySelect_FollowsTableOrderBeforePathOrder()
    {
        HidCandidate later = Col03 with { Path = "z" };
        HidCandidate earlierPath = Col03 with { Path = "a", ProductId = 0x0696 };

        Assert.True(InterfaceSelector.TrySelect([earlierPath, later], SupportedDevices.All, out HidCandidate selected));
        Assert.Equal(later, selected);
    }

    [Fact]
    public void TrySelect_ReportsNoneWhenNothingMatches()
    {
        Assert.False(InterfaceSelector.TrySelect([], SupportedDevices.All, out _));
    }
}
