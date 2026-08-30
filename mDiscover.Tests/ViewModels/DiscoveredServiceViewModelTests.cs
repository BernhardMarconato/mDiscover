using System.Net;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Services;
using mDiscover.ViewModels;
using NSubstitute;

namespace mDiscover.Tests.ViewModels;

public class DiscoveredServiceViewModelTests
{
    private readonly IClipboardService _clipboard = Substitute.For<IClipboardService>();
    private readonly IUriLauncherService _launcher = Substitute.For<IUriLauncherService>();
    private readonly IServiceDiscoveryEngine _engine = Substitute.For<IServiceDiscoveryEngine>();
    private readonly IExportService _exportService = ExportService.Default;

    [Fact]
    public void Properties_MapCorrectlyFromModel()
    {
        var model = new DiscoveredService
        {
            Id = "vm-test-1",
            InstanceName = "Living Room Hub",
            ServiceType = "_hap._tcp",
            Domain = "local",
            HostName = "hub.local",
            Port = 51827,
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.120")]
        };

        var vm = new DiscoveredServiceViewModel(model, _clipboard, _launcher, _engine, _exportService);

        Assert.Equal("Living Room Hub", vm.InstanceName);
        Assert.Equal("_hap._tcp", vm.ServiceType);
        Assert.Equal("hub.local", vm.HostName);
        Assert.Equal(51827, vm.Port);
        Assert.True(vm.HasHostName);
    }

    [Theory]
    [InlineData("_http._tcp", 80, "192.168.1.1", "http://192.168.1.1")]
    [InlineData("_https._tcp", 443, "192.168.1.1", "https://192.168.1.1")]
    [InlineData("_elg._tcp", 9123, "192.168.0.206", "http://192.168.0.206:9123")]
    [InlineData("_octoprint._tcp", 5000, "192.168.1.50", "http://192.168.1.50:5000")]
    public void CanOpenInBrowser_TrueForWebAndHttpServices(string serviceType, int port, string ip, string expectedUrl)
    {
        var model = new DiscoveredService
        {
            Id = "vm-test-web",
            InstanceName = "Web Service",
            ServiceType = serviceType,
            Domain = "local",
            Port = port,
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse(ip)]
        };

        var vm = new DiscoveredServiceViewModel(model, _clipboard, _launcher, _engine, _exportService);

        Assert.True(vm.CanOpenInBrowser);
        Assert.Equal(expectedUrl, vm.BrowserUrl);
    }

    [Fact]
    public void CopyCommand_InvokesClipboardService()
    {
        var model = new DiscoveredService
        {
            Id = "vm-test-3",
            InstanceName = "SSH Server",
            ServiceType = "_ssh._tcp",
            Domain = "local",
            HostName = "server.local",
            Port = 22,
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.200")]
        };

        var vm = new DiscoveredServiceViewModel(model, _clipboard, _launcher, _engine, _exportService);
        vm.CopyCommand.Execute(ExportFormat.Json);

        _clipboard.Received(1).SetText(Arg.Is<string>(s => s.Contains("SSH Server")));
    }

    [Fact]
    public void UpdateFromModel_IncrementallySyncsIpAddressesAndTxtRecords()
    {
        var model = new DiscoveredService
        {
            Id = "vm-test-sync",
            InstanceName = "Smart Lamp",
            ServiceType = "_hue._tcp",
            Domain = "local",
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.100")],
            TxtRecords = [new TxtRecordItem("model", "LCT001")]
        };

        var vm = new DiscoveredServiceViewModel(model, _clipboard, _launcher, _engine, _exportService);
        Assert.Single(vm.IPv4Addresses);
        Assert.Single(vm.TxtRecords);
        Assert.Single(vm.AllIpAddresses);

        // Add an IPv6 and a new TXT record
        model.IPv6Addresses.Add(IPAddress.Parse("fe80::100"));
        model.TxtRecords.Add(new TxtRecordItem("version", "2"));
        vm.UpdateFromModel();

        Assert.Single(vm.IPv4Addresses);
        Assert.Single(vm.IPv6Addresses);
        Assert.Equal(2, vm.AllIpAddresses.Count);
        Assert.Equal(2, vm.TxtRecords.Count);
    }
}

