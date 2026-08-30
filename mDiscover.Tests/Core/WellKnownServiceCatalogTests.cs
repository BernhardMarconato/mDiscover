using mDiscover.Core.Models;

namespace mDiscover.Tests.Core;

public class WellKnownServiceCatalogTests
{
    [Theory]
    [InlineData("_http._tcp", "Web Server (HTTP)", ServiceCategory.WebAndApi, 80)]
    [InlineData("_ssh._tcp", "SSH Remote Terminal", ServiceCategory.RemoteAccess, 22)]
    [InlineData("_hap._tcp", "Apple HomeKit", ServiceCategory.SmartHomeAndIot, null)]
    [InlineData("_airplay._tcp", "Apple AirPlay Display", ServiceCategory.MediaAndAudio, 7000)]
    [InlineData("_octoprint._tcp", "OctoPrint 3D Print Server", ServiceCategory.PrintingAndWorkshop, 80)]
    [InlineData("_ipp._tcp", "IPP Network Printer", ServiceCategory.PrintAndScan, 631)]
    public void GetOrInfer_ReturnsKnownDefinitions(string serviceType, string expectedDisplayName, ServiceCategory expectedCategory, int? expectedPort)
    {
        var definition = WellKnownServiceCatalog.GetOrInfer(serviceType);

        Assert.Equal(expectedDisplayName, definition.DisplayName);
        Assert.Equal(expectedCategory, definition.Category);
        Assert.Equal(expectedPort, definition.DefaultPort);
    }

    [Fact]
    public void GetOrInfer_InfersGenericTypeNameForUnknownType()
    {
        var definition = WellKnownServiceCatalog.GetOrInfer("_custom-sensor._udp");

        Assert.Equal("Custom-sensor (UDP)", definition.DisplayName);
        Assert.Equal(ServiceCategory.Other, definition.Category);
    }

    [Theory]
    [InlineData("_my-web-api._tcp", ServiceCategory.WebAndApi)]
    [InlineData("_custom-audio-cast._tcp", ServiceCategory.MediaAndAudio)]
    [InlineData("_smart-light-node._tcp", ServiceCategory.SmartHomeAndIot)]
    [InlineData("_fast-scanner._tcp", ServiceCategory.PrintAndScan)]
    [InlineData("_custom-backup-disk._tcp", ServiceCategory.StorageAndFiles)]
    public void InferCategory_CategorizesFuzzyMatches(string serviceType, ServiceCategory expectedCategory)
    {
        var category = WellKnownServiceCatalog.InferCategory(serviceType);

        Assert.Equal(expectedCategory, category);
    }
}

