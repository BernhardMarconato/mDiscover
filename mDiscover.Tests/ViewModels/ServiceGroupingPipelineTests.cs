using System.Collections.ObjectModel;
using System.Net;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Services;
using mDiscover.ViewModels;
using mDiscover.ViewModels.Services;
using NSubstitute;

namespace mDiscover.Tests.ViewModels;

public class ServiceGroupingPipelineTests
{
    private static readonly IClipboardService _clipboard = Substitute.For<IClipboardService>();
    private static readonly IUriLauncherService _launcher = Substitute.For<IUriLauncherService>();
    private static readonly IServiceDiscoveryEngine _engine = Substitute.For<IServiceDiscoveryEngine>();
    private static readonly IExportService _export = ExportService.Default;

    private static DiscoveredServiceViewModel CreateService(
        string id,
        string instanceName,
        string serviceType,
        string ip,
        int port = 80,
        string? hostName = null,
        List<TxtRecordItem>? txt = null)
    {
        var model = new DiscoveredService
        {
            Id = id,
            InstanceName = instanceName,
            ServiceType = serviceType,
            Domain = "local",
            ProviderId = "test",
            IPv4Addresses = [IPAddress.Parse(ip)],
            Port = port,
            HostName = hostName ?? $"{instanceName}.local",
            TxtRecords = txt ?? []
        };

        return new DiscoveredServiceViewModel(model, _clipboard, _launcher, _engine, _export);
    }

    [Fact]
    public void Filter_WithSearchText_MatchesInstanceNameAndTypeAndIp()
    {
        var services = new List<DiscoveredServiceViewModel>
        {
            CreateService("1", "Living Room Speaker", "_airplay._tcp", "192.168.1.10"),
            CreateService("2", "Philips Hue Bridge", "_hue._tcp", "192.168.1.20"),
            CreateService("3", "Office Printer", "_ipp._tcp", "192.168.1.30")
        };

        var filteredByName = ServiceGroupingPipeline.Filter(services, "Hue", null).ToList();
        Assert.Single(filteredByName);
        Assert.Equal("Philips Hue Bridge", filteredByName[0].InstanceName);

        var filteredByIp = ServiceGroupingPipeline.Filter(services, "1.30", null).ToList();
        Assert.Single(filteredByIp);
        Assert.Equal("Office Printer", filteredByIp[0].InstanceName);

        var filteredByType = ServiceGroupingPipeline.Filter(services, "_airplay", null).ToList();
        Assert.Single(filteredByType);
        Assert.Equal("Living Room Speaker", filteredByType[0].InstanceName);
    }

    [Fact]
    public void Filter_WithCategory_MatchesCategory()
    {
        var services = new List<DiscoveredServiceViewModel>
        {
            CreateService("1", "Living Room Speaker", "_airplay._tcp", "192.168.1.10"),
            CreateService("2", "Philips Hue Bridge", "_hue._tcp", "192.168.1.20"),
            CreateService("3", "Office Printer", "_ipp._tcp", "192.168.1.30")
        };

        // _airplay._tcp is MediaAndAudio, _hue._tcp is SmartHomeAndIot, _ipp._tcp is PrintingAndImaging
        var filtered = ServiceGroupingPipeline.Filter(services, null, ServiceCategory.SmartHomeAndIot).ToList();
        Assert.Single(filtered);
        Assert.Equal("Philips Hue Bridge", filtered[0].InstanceName);
    }

    [Fact]
    public void SyncCollections_GroupingByServiceType_CreatesServiceTypeGroups()
    {
        var services = new List<DiscoveredServiceViewModel>
        {
            CreateService("1", "Speaker A", "_airplay._tcp", "192.168.1.10"),
            CreateService("2", "Speaker B", "_airplay._tcp", "192.168.1.11"),
            CreateService("3", "Printer", "_ipp._tcp", "192.168.1.30")
        };

        var targetGroups = new ObservableCollection<ServiceGroupViewModel>();
        var targetFiltered = new ObservableCollection<DiscoveredServiceViewModel>();

        ServiceGroupingPipeline.SyncCollections(
            targetGroups,
            targetFiltered,
            services,
            GroupingMode.ByServiceType,
            ServiceSortMode.Name,
            false);

        Assert.Equal(2, targetGroups.Count);
        Assert.Equal(3, targetFiltered.Count);

        var airplayGroup = targetGroups.FirstOrDefault(g => g.Key == "_airplay._tcp");
        Assert.NotNull(airplayGroup);
        Assert.Equal(2, airplayGroup.Count);
    }

    [Fact]
    public void SyncCollections_GroupingByHost_GroupsByIpAddress()
    {
        var services = new List<DiscoveredServiceViewModel>
        {
            CreateService("1", "Web Server", "_http._tcp", "192.168.1.50", port: 80),
            CreateService("2", "Secure Web", "_https._tcp", "192.168.1.50", port: 443),
            CreateService("3", "SSH Server", "_ssh._tcp", "192.168.1.60", port: 22)
        };

        var targetGroups = new ObservableCollection<ServiceGroupViewModel>();
        var targetFiltered = new ObservableCollection<DiscoveredServiceViewModel>();

        ServiceGroupingPipeline.SyncCollections(
            targetGroups,
            targetFiltered,
            services,
            GroupingMode.ByHost,
            ServiceSortMode.Name,
            false);

        Assert.Equal(2, targetGroups.Count);
        Assert.Equal(3, targetFiltered.Count);

        var host1 = targetGroups.FirstOrDefault(g => g.Key == "192.168.1.50");
        Assert.NotNull(host1);
        Assert.Equal(2, host1.Count);
    }
}
