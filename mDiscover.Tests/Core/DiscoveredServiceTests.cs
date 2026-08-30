using System.Net;
using mDiscover.Core.Models;

namespace mDiscover.Tests.Core;

public class DiscoveredServiceTests
{
    [Fact]
    public void PrimaryIpAddress_PrefersIPv4OverIPv6()
    {
        var ipv4 = IPAddress.Parse("192.168.1.50");
        var ipv6 = IPAddress.Parse("fe80::1");

        var service = new DiscoveredService
        {
            Id = "test-1",
            InstanceName = "Living Room Apple TV",
            ServiceType = "_airplay._tcp",
            Domain = "local",
            ProviderId = "test-provider",
            IPv4Addresses = [ipv4],
            IPv6Addresses = [ipv6]
        };

        Assert.Equal(ipv4, service.PrimaryIpAddress);
        Assert.Equal("192.168.1.50", service.PrimaryIp);
    }

    [Fact]
    public void PrimaryIpAddress_FallsBackToIPv6WhenIPv4IsEmpty()
    {
        var ipv6 = IPAddress.Parse("fe80::1");

        var service = new DiscoveredService
        {
            Id = "test-2",
            InstanceName = "Printer",
            ServiceType = "_ipp._tcp",
            Domain = "local",
            ProviderId = "test-provider",
            IPv4Addresses = [],
            IPv6Addresses = [ipv6]
        };

        Assert.Equal(ipv6, service.PrimaryIpAddress);
        Assert.Equal("fe80::1", service.PrimaryIp);
    }

    [Fact]
    public void FormattedEndpoint_IncludesIpAndPortWhenAvailable()
    {
        var service = new DiscoveredService
        {
            Id = "test-3",
            InstanceName = "Web Server",
            ServiceType = "_http._tcp",
            Domain = "local",
            ProviderId = "test-provider",
            IPv4Addresses = [IPAddress.Parse("10.0.0.5")],
            Port = 8080
        };

        Assert.Equal("10.0.0.5:8080", service.FormattedEndpoint);
    }

    [Fact]
    public void FormattedEndpoint_FallsBackToHostNameWhenIpUnavailable()
    {
        var service = new DiscoveredService
        {
            Id = "test-4",
            InstanceName = "NAS Server",
            ServiceType = "_smb._tcp",
            Domain = "local",
            ProviderId = "test-provider",
            HostName = "nas.local",
            Port = 445
        };

        Assert.Equal("nas.local:445", service.FormattedEndpoint);
    }

    [Theory]
    [InlineData("_http._tcp", 80, "192.168.1.1", "http://192.168.1.1")]
    [InlineData("_https._tcp", 443, "192.168.1.1", "https://192.168.1.1")]
    [InlineData("_elg._tcp", 9123, "192.168.0.206", "http://192.168.0.206:9123")]
    [InlineData("_octoprint._tcp", 5000, "192.168.1.50", "http://192.168.1.50:5000")]
    public void CanOpenInBrowser_GeneratesCorrectUrl(string serviceType, int port, string ip, string expectedUrl)
    {
        var service = new DiscoveredService
        {
            Id = "test-web",
            InstanceName = "Web Service",
            ServiceType = serviceType,
            Domain = "local",
            Port = port,
            ProviderId = "test-provider",
            IPv4Addresses = [IPAddress.Parse(ip)]
        };

        Assert.True(service.CanOpenInBrowser);
        Assert.Equal(expectedUrl, service.BrowserUrl);
    }

    [Fact]
    public void FullServicePath_FormatsCorrectly()
    {
        var service = new DiscoveredService
        {
            Id = "test-path",
            InstanceName = "My Printer",
            ServiceType = "_ipp._tcp",
            Domain = "local",
            ProviderId = "test-provider"
        };

        Assert.Equal("My Printer._ipp._tcp.local", service.FullServicePath);
    }
}

