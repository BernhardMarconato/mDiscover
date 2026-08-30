using mDiscover.Core.Common;

namespace mDiscover.Tests.Core;

public class AppInfoTests
{
    [Fact]
    public void VersionString_IsNotEmpty()
    {
        var versionStr = AppInfo.VersionString;
        Assert.False(string.IsNullOrWhiteSpace(versionStr));
    }

    [Fact]
    public void DisplayVersionString_IsNotEmptyAndContainsVersion()
    {
        var displayVer = AppInfo.DisplayVersionString;
        Assert.False(string.IsNullOrWhiteSpace(displayVer));
        Assert.Contains(AppInfo.VersionString, displayVer);

        var commitHash = AppInfo.CommitHash;
        if (commitHash != null)
        {
            Assert.NotEmpty(commitHash);
            Assert.Equal(commitHash.Length > 7 ? commitHash[..7] : commitHash, AppInfo.ShortCommitHash);
        }
    }

    [Fact]
    public void Version_IsNotNullAndValid()
    {
        var version = AppInfo.Version;
        Assert.NotNull(version);
        Assert.True(version.Major >= 1);
    }

    [Theory]
    [InlineData("http", "_http._tcp")]
    [InlineData("_http", "_http._tcp")]
    [InlineData("_http._tcp", "_http._tcp")]
    [InlineData("ipp._tcp", "_ipp._tcp")]
    [InlineData("spotify-connect._tcp", "_spotify-connect._tcp")]
    [InlineData("_custom._udp", "_custom._udp")]
    [InlineData("custom._udp", "_custom._udp")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizeServiceType_NormalizesCorrectly(string input, string expected)
    {
        var result = AppInfo.NormalizeServiceType(input);
        Assert.Equal(expected, result);
    }
}
