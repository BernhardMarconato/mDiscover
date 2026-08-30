using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Services;
using mDiscover.ViewModels;
using mDiscover.ViewModels.Services;
using NSubstitute;

namespace mDiscover.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IServiceDiscoveryEngine _engine = Substitute.For<IServiceDiscoveryEngine>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IDispatcherService _dispatcher = Substitute.For<IDispatcherService>();
    private readonly IDnsSdDiscoveryProvider _provider = Substitute.For<IDnsSdDiscoveryProvider>();
    private readonly IClipboardService _clipboard = Substitute.For<IClipboardService>();
    private readonly IUriLauncherService _launcher = Substitute.For<IUriLauncherService>();
    private readonly IAppLifecycleService _lifecycle = Substitute.For<IAppLifecycleService>();
    private readonly IAppPathService _pathService = Substitute.For<IAppPathService>();
    private readonly IExportService _exportService = ExportService.Default;
    private readonly ILogger<MainViewModel> _logger = NullLogger<MainViewModel>.Instance;
    private readonly ILogger<DiscoveredServiceRegistry> _registryLogger = NullLogger<DiscoveredServiceRegistry>.Instance;

    private MainViewModel CreateViewModel()
    {
        var registry = new DiscoveredServiceRegistry(_engine, _dispatcher, _clipboard, _launcher, _exportService, _registryLogger);
        return new MainViewModel(_engine, _settingsService, _dispatcher, _clipboard, _launcher, _lifecycle, _pathService, _exportService, _logger, registry);
    }

    public MainViewModelTests()
    {
        _provider.SupportsWildcardDiscovery.Returns(true);

        _engine.ActiveProvider.Returns(_provider);
        _engine.AvailableProviders.Returns(new[] { _provider });
        _engine.GetProviderId(_provider).Returns("win32");

        // Default mock settings
        _settingsService.ReadSetting(SettingDefinitions.DiscoveryMode).Returns(DiscoveryMode.WildcardMeta);
        _settingsService.ReadSetting(SettingDefinitions.SidebarWidth).Returns(360.0);
        _settingsService.ReadSetting(SettingDefinitions.SortMode).Returns(ServiceSortMode.Name);
        _settingsService.ReadSetting(SettingDefinitions.IsSortDescending).Returns(false);
        _settingsService.ReadSetting(SettingDefinitions.GroupingMode).Returns(GroupingMode.ByHost);
        _settingsService.ReadSetting(SettingDefinitions.AppTheme).Returns(AppTheme.Default);
        _settingsService.ReadSetting(SettingDefinitions.EnabledServiceTypes).Returns(new List<string>());
        _settingsService.ReadSetting(SettingDefinitions.CustomServiceTypes).Returns(new List<string>());
        _settingsService.ReadSetting(SettingDefinitions.PreferredProvider).Returns("win32");
        _settingsService.ReadSetting(SettingDefinitions.DefaultExportFormat).Returns(ExportFormat.Markdown);

        // Synchronous dispatcher for tests
        _dispatcher.When(d => d.Enqueue(Arg.Any<Action>())).Do(callInfo => callInfo.Arg<Action>()());
    }

    [Fact]
    public void Constructor_InitializesStateFromSettings()
    {
        var vm = CreateViewModel();

        Assert.Equal(DiscoveryMode.WildcardMeta, vm.DiscoveryMode);
        Assert.Equal(360.0, vm.Settings.SidebarWidth);
        Assert.Equal(ServiceSortMode.Name, vm.SortMode);
        Assert.False(vm.IsSortDescending);
        Assert.Equal(GroupingMode.ByHost, vm.Grouping);
        Assert.Null(vm.SelectedService);
    }

    [Fact]
    public async Task ServiceDiscovered_AddsServiceAndUpdatesCounts()
    {
        var vm = CreateViewModel();

        var service = new DiscoveredService
        {
            Id = "srv-1",
            InstanceName = "Hue Bridge",
            ServiceType = "_hue._tcp",
            Domain = "local",
            HostName = "hue.local",
            Port = 80,
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.55")]
        };

        // Raise engine event
        _engine.ServiceDiscovered += Raise.Event<EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>>(_provider, new ServiceDiscoveredEventArgs(service));

        // Wait for debounced UI update with xUnit v3 TestContext cancellation token
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(1, vm.Stats.ServicesCount);
        Assert.Single(vm.FilteredServices);
        Assert.Equal("Hue Bridge", vm.FilteredServices[0].InstanceName);
    }

    [Fact]
    public async Task SearchText_FiltersDiscoveredServices()
    {
        var vm = CreateViewModel();

        var service1 = new DiscoveredService
        {
            Id = "srv-1",
            InstanceName = "Living Room TV",
            ServiceType = "_airplay._tcp",
            Domain = "local",
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.10")]
        };

        var service2 = new DiscoveredService
        {
            Id = "srv-2",
            InstanceName = "Office Printer",
            ServiceType = "_ipp._tcp",
            Domain = "local",
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.20")]
        };

        _engine.ServiceDiscovered += Raise.Event<EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>>(_provider, new ServiceDiscoveredEventArgs(service1));
        _engine.ServiceDiscovered += Raise.Event<EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>>(_provider, new ServiceDiscoveredEventArgs(service2));

        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(2, vm.FilteredServices.Count);

        // Apply search filter
        vm.SearchText = "Printer";
        Assert.Single(vm.FilteredServices);
        Assert.Equal("Office Printer", vm.FilteredServices[0].InstanceName);

        // Clear filter
        vm.SearchText = string.Empty;
        Assert.Equal(2, vm.FilteredServices.Count);
    }

    [Fact]
    public async Task SelectedService_TracksSelectionState()
    {
        var vm = CreateViewModel();

        var service = new DiscoveredService
        {
            Id = "srv-1",
            InstanceName = "MacBook Pro",
            ServiceType = "_ssh._tcp",
            Domain = "local",
            ProviderId = "win32",
            IPv4Addresses = [IPAddress.Parse("192.168.1.30")]
        };

        _engine.ServiceDiscovered += Raise.Event<EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>>(_provider, new ServiceDiscoveredEventArgs(service));

        await Task.Delay(100, TestContext.Current.CancellationToken);

        var target = vm.FilteredServices[0];
        vm.SelectedService = target;

        Assert.True(target.IsSelected);
        Assert.NotNull(vm.SelectedService);
        Assert.Same(target, vm.SelectedService);
    }
}

