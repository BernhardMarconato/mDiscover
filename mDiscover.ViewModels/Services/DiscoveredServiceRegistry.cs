using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;

namespace mDiscover.ViewModels.Services;

/// <summary>
/// Manages the live dictionary of discovered DNS-SD services, listens to engine events,
/// handles debounced UI synchronization, and maintains observable presentation collections.
/// </summary>
public partial class DiscoveredServiceRegistry : ObservableObject, IDisposable
{
    private readonly IServiceDiscoveryEngine _engine;
    private readonly IDispatcherService _dispatcher;
    private readonly IClipboardService _clipboardService;
    private readonly IUriLauncherService _launcherService;
    private readonly IExportService _exportService;
    private readonly ILogger<DiscoveredServiceRegistry> _logger;

    private readonly ConcurrentDictionary<string, DiscoveredServiceViewModel> _servicesMap = new(StringComparer.OrdinalIgnoreCase);
    private int _isUiUpdateScheduled;

    // Last filter state cached for automatic background re-filtering on discovery events
    private string? _currentSearchText;
    private ServiceCategory? _currentCategory;
    private GroupingMode _currentGrouping = GroupingMode.ByHost;
    private ServiceSortMode _currentSortMode = ServiceSortMode.Name;
    private bool _currentIsSortDescending;
    private bool _isDiscovering;

    public ObservableCollection<DiscoveredServiceViewModel> FilteredServices { get; } = [];
    public ObservableCollection<ServiceGroupViewModel> GroupedServices { get; } = [];
    public ObservableCollection<ServiceCategory> Categories { get; } = [];

    [ObservableProperty]
    public partial DiscoveryStats Stats { get; private set; } = new();

    [ObservableProperty]
    public partial bool HasDiscoveredItems { get; private set; }

    [ObservableProperty]
    public partial bool IsInitialDiscoveryLoading { get; private set; }

    [ObservableProperty]
    public partial bool IsNoSearchResults { get; private set; }

    public ICollection<DiscoveredServiceViewModel> AllServices => _servicesMap.Values;

    public DiscoveredServiceRegistry(
        IServiceDiscoveryEngine engine,
        IDispatcherService dispatcher,
        IClipboardService clipboardService,
        IUriLauncherService launcherService,
        IExportService exportService,
        ILogger<DiscoveredServiceRegistry> logger)
    {
        _engine = engine;
        _dispatcher = dispatcher;
        _clipboardService = clipboardService;
        _launcherService = launcherService;
        _exportService = exportService;
        _logger = logger;

        _engine.ServiceDiscovered += OnServiceDiscovered;
        _engine.ServiceUpdated += OnServiceUpdated;
        _engine.ServiceLost += OnServiceLost;
    }

    /// <summary>
    /// Applies the specified filter, sort, and grouping parameters to the active services collection.
    /// </summary>
    public void ApplyFilters(
        string? searchText,
        ServiceCategory? category,
        GroupingMode grouping,
        ServiceSortMode sortMode,
        bool isSortDescending,
        bool isDiscovering)
    {
        _currentSearchText = searchText;
        _currentCategory = category;
        _currentGrouping = grouping;
        _currentSortMode = sortMode;
        _currentIsSortDescending = isSortDescending;
        _isDiscovering = isDiscovering;

        ApplyFiltersAndSync();
    }

    /// <summary>
    /// Updates the discovery state for loading/empty state calculations.
    /// </summary>
    public void SetDiscoveringState(bool isDiscovering)
    {
        _isDiscovering = isDiscovering;
        UpdateEmptyAndLoadingStates();
    }

    /// <summary>
    /// Clears all discovered services and resets presentation collections.
    /// </summary>
    public void Clear()
    {
        _servicesMap.Clear();
        FilteredServices.Clear();
        GroupedServices.Clear();
        Categories.Clear();
        UpdateStats();
        UpdateEmptyAndLoadingStates();
    }

    private void OnServiceDiscovered(IDnsSdDiscoveryProvider? sender, ServiceDiscoveredEventArgs e)
    {
        _logger.LogDebug("Service discovered: {Id} ({Type})", e.Service.Id, e.Service.ServiceType);
        var vm = new DiscoveredServiceViewModel(e.Service, _clipboardService, _launcherService, _engine, _exportService);
        _servicesMap[e.Service.Id] = vm;
        ScheduleUiUpdate();
    }

    private void OnServiceUpdated(IDnsSdDiscoveryProvider? sender, ServiceUpdatedEventArgs e)
    {
        _logger.LogDebug("Service updated: {Id} ({Type})", e.Service.Id, e.Service.ServiceType);
        if (_servicesMap.TryGetValue(e.Service.Id, out var existing))
        {
            _dispatcher.Enqueue(() => existing.UpdateFromModel());
        }
        else
        {
            var vm = new DiscoveredServiceViewModel(e.Service, _clipboardService, _launcherService, _engine, _exportService);
            _servicesMap[e.Service.Id] = vm;
        }
        ScheduleUiUpdate();
    }

    private void OnServiceLost(IDnsSdDiscoveryProvider? sender, ServiceLostEventArgs e)
    {
        _logger.LogInformation("Service lost: {Id}", e.ServiceId);
        if (_servicesMap.TryGetValue(e.ServiceId, out var vm))
        {
            _dispatcher.Enqueue(() => vm.IsOnline = false);
            ScheduleUiUpdate();
        }
    }

    private void ScheduleUiUpdate()
    {
        if (Interlocked.CompareExchange(ref _isUiUpdateScheduled, 1, 0) == 0)
        {
            Task.Delay(50).ContinueWith(_ =>
            {
                _dispatcher.Enqueue(() =>
                {
                    Interlocked.Exchange(ref _isUiUpdateScheduled, 0);
                    ApplyFiltersAndSync();
                    UpdateStats();
                });
            });
        }
    }

    private void ApplyFiltersAndSync()
    {
        var filtered = ServiceGroupingPipeline.Filter(_servicesMap.Values, _currentSearchText, _currentCategory).ToList();

        ServiceGroupingPipeline.SyncCollections(
            GroupedServices,
            FilteredServices,
            filtered,
            _currentGrouping,
            _currentSortMode,
            _currentIsSortDescending);

        UpdateEmptyAndLoadingStates();
    }

    private void UpdateEmptyAndLoadingStates()
    {
        HasDiscoveredItems = FilteredServices.Count > 0;
        IsInitialDiscoveryLoading = FilteredServices.Count == 0 && _servicesMap.IsEmpty && _isDiscovering;
        IsNoSearchResults = FilteredServices.Count == 0 && !IsInitialDiscoveryLoading;
    }

    private void UpdateStats()
    {
        var servicesCount = _servicesMap.Values.Count(s => s.IsOnline);
        var typesCount = _servicesMap.Values.Where(s => s.IsOnline).Select(s => s.ServiceType).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var hostsCount = _servicesMap.Values.Where(s => s.IsOnline).Select(s => s.PrimaryIp).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Stats = new DiscoveryStats(servicesCount, typesCount, hostsCount);

        var cats = _servicesMap.Values
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c.ToString())
            .ToList();

        if (!cats.SequenceEqual(Categories))
        {
            Categories.Clear();
            foreach (var c in cats)
                Categories.Add(c);
        }

        UpdateEmptyAndLoadingStates();
    }

    public void Dispose()
    {
        _engine.ServiceDiscovered -= OnServiceDiscovered;
        _engine.ServiceUpdated -= OnServiceUpdated;
        _engine.ServiceLost -= OnServiceLost;
        GC.SuppressFinalize(this);
    }
}
