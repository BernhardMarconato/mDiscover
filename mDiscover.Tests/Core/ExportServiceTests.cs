using System.Net;
using mDiscover.Core.Models;
using mDiscover.Core.Services;

namespace mDiscover.Tests.Core;

public class ExportServiceTests
{
    private readonly ExportService _exportService;

    public ExportServiceTests()
    {
        _exportService = new ExportService();
    }

    private static DiscoveredService CreateSampleService() => new()
    {
        Id = "sample-1",
        InstanceName = "OctoPrint 3D Printer",
        ServiceType = "_octoprint._tcp",
        Domain = "local",
        HostName = "octopi.local",
        Port = 5000,
        ProviderId = "win32",
        IPv4Addresses = [IPAddress.Parse("192.168.1.100")],
        TxtRecords =
        [
            new TxtRecordItem("version", "1.9.0"),
            new TxtRecordItem("path", "/api")
        ]
    };

    [Fact]
    public void ToJson_SerializesServiceCorrectly()
    {
        var service = CreateSampleService();

        var json = _exportService.ToJson(service);

        Assert.NotNull(json);
        Assert.Contains("OctoPrint 3D Printer", json);
        Assert.Contains("_octoprint._tcp", json);
        Assert.Contains("192.168.1.100", json);
    }

    [Fact]
    public void ToPlainText_FormatsAllKeyFields()
    {
        var service = CreateSampleService();

        var text = _exportService.ToPlainText(service);

        Assert.Contains("OctoPrint 3D Printer", text);
        Assert.Contains("_octoprint._tcp", text);
        Assert.Contains("octopi.local", text);
        Assert.Contains("192.168.1.100:5000", text);
        Assert.Contains("version = 1.9.0", text);
        Assert.Contains("path = /api", text);
    }

    [Fact]
    public void ToMarkdown_FormatsSingleServiceWithCodeBlocks()
    {
        var service = CreateSampleService();

        var md = _exportService.ToMarkdown(service);

        Assert.Contains("### OctoPrint 3D Printer", md);
        Assert.Contains("`_octoprint._tcp`", md);
        Assert.Contains("`192.168.1.100:5000`", md);
        Assert.Contains("`version`: 1.9.0", md);
    }

    [Fact]
    public void ToCsv_EscapesCommasAndQuotes()
    {
        var service = new DiscoveredService
        {
            Id = "sample-quotes",
            InstanceName = "My \"Special\" Printer, 2nd Floor",
            ServiceType = "_ipp._tcp",
            Domain = "local",
            ProviderId = "win32",
            Port = 631
        };

        var csv = _exportService.ToCsv(new[] { service });

        Assert.Contains("\"My \"\"Special\"\" Printer, 2nd Floor\"", csv);
        Assert.Contains("_ipp._tcp", csv);
    }
}

