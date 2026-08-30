using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.ViewModels.Services;

namespace mDiscover.ViewModels;

/// <summary>
/// Primary application ViewModel coordinating discovery lifecycle, sidebar navigation, real-time filtering, and service presentation.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IServiceDiscoveryEngine _engine;
    private readonly ISettingsService _settingsService;
    private readonly IDispatcherService _dispatcher;
    private readonly IClipboardService _clipboardService;
    private readonly IExportService _exportService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly DiscoveredServiceRegistry _registry;

    public SettingsViewModel Settings { get; }

    public ObservableCollection<DiscoveredServiceViewModel> FilteredServices => _registry.FilteredServices;
    public ObservableCollection<ServiceGroupViewModel> GroupedServices => _registry.GroupedServices;
    public ObservableCollection<ServiceCategory> Categories => _registry.Categories;

    public DiscoveryStats Stats => _registry.Stats;
    public bool HasDiscoveredItems => _registry.HasDiscoveredItems;
    public bool IsInitialDiscoveryLoading => _registry.IsInitialDiscoveryLoading;
    public bool IsNoSearchResults => _registry.IsNoSearchResults;

    public DiscoveryMode DiscoveryMode => Settings.DiscoveryMode;
    public IDnsSdDiscoveryProvider ActiveProvider => _engine.ActiveProvider;

    [ObservableProperty]
    public partial ExportFormat SelectedExportFormat { get; set; } = ExportFormat.Markdown;

    partial void OnSelectedExportFormatChanged(ExportFormat value)
    {
        _settingsService.SaveSetting(SettingDefinitions.DefaultExportFormat, value);
    }

    [ObservableProperty]
    public partial bool IsExportNotificationOpen { get; set; }

    [ObservableProperty]
    public partial int ExportNotificationCount { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFiltersAndGrouping();

    [ObservableProperty]
    public partial ServiceCategory? SelectedCategory { get; set; }

    partial void OnSelectedCategoryChanged(ServiceCategory? value) => ApplyFiltersAndGrouping();

    [ObservableProperty]
    public partial GroupingMode Grouping { get; set; } = GroupingMode.ByHost;

    partial void OnGroupingChanged(GroupingMode value)
    {
        _settingsService.SaveSetting(SettingDefinitions.GroupingMode, value);
        ApplyFiltersAndGrouping();
    }

    [ObservableProperty]
    public partial DiscoveredServiceViewModel? SelectedService { get; set; }

    partial void OnSelectedServiceChanged(DiscoveredServiceViewModel? oldValue, DiscoveredServiceViewModel? newValue)
    {
        if (oldValue != null)
            oldValue.IsSelected = false;
        if (newValue != null)
            newValue.IsSelected = true;
    }

    [ObservableProperty]
    public partial bool IsDiscovering { get; set; }

    partial void OnIsDiscoveringChanged(bool value) => _registry.SetDiscoveringState(value);

    [ObservableProperty]
    public partial DiscoveryState DiscoveryState { get; set; } = DiscoveryState.Idle;

    partial void OnDiscoveryStateChanged(DiscoveryState value) => _registry.SetDiscoveringState(IsDiscovering || value == DiscoveryState.Discovering);

    [ObservableProperty]
    public partial string? StatusError { get; set; }

    [ObservableProperty]
    public partial ServiceSortMode SortMode { get; set; } = ServiceSortMode.Name;

    partial void OnSortModeChanged(ServiceSortMode value)
    {
        _settingsService.SaveSetting(SettingDefinitions.SortMode, value);
        ApplyFiltersAndGrouping();
    }

    [ObservableProperty]
    public partial bool IsSortDescending { get; set; }

    partial void OnIsSortDescendingChanged(bool value)
    {
        _settingsService.SaveSetting(SettingDefinitions.IsSortDescending, value);
        ApplyFiltersAndGrouping();
    }

    public MainViewModel(
        IServiceDiscoveryEngine engine,
        ISettingsService settingsService,
        IDispatcherService dispatcher,
        IClipboardService clipboardService,
        IUriLauncherService launcherService,
        IAppLifecycleService lifecycleService,
        IAppPathService appPathService,
        IExportService exportService,
        ILogger<MainViewModel> logger,
        DiscoveredServiceRegistry registry)
    {
        _engine = engine;
        _settingsService = settingsService;
        _dispatcher = dispatcher;
        _clipboardService = clipboardService;
        _exportService = exportService;
        _logger = logger;
        _registry = registry;

        _registry.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                OnPropertyChanged(e.PropertyName);
        };

        SortMode = _settingsService.ReadSetting(SettingDefinitions.SortMode);
        IsSortDescending = _settingsService.ReadSetting(SettingDefinitions.IsSortDescending);
        Grouping = _settingsService.ReadSetting(SettingDefinitions.GroupingMode);
        SelectedExportFormat = _settingsService.ReadSetting(SettingDefinitions.DefaultExportFormat);

        Settings = new SettingsViewModel(settingsService, engine, RestartDiscoveryAsync, launcherService, lifecycleService, appPathService);

        _engine.StateChanged += OnEngineStateChanged;

        _logger.LogInformation("MainViewModel initialized with active provider: {Provider}", _engine.GetProviderId(ActiveProvider));
    }

    private void ApplyFiltersAndGrouping()
    {
        _registry.ApplyFilters(
            SearchText,
            SelectedCategory,
            Grouping,
            SortMode,
            IsSortDescending,
            IsDiscovering || DiscoveryState == DiscoveryState.Discovering);
    }

    [RelayCommand]
    public void DismissExportNotification() => IsExportNotificationOpen = false;

    /// <summary>
    /// Exports all currently filtered services using the specified format, or defaults to <see cref="SelectedExportFormat"/>.
    /// </summary>
    [RelayCommand]
    public void Export(ExportFormat? format = null)
    {
        var targetFormat = format ?? SelectedExportFormat;
        SelectedExportFormat = targetFormat;
        var services = FilteredServices.Select(s => s.Model).ToList();
        var output = _exportService.Export(services, targetFormat);

        _clipboardService.SetText(output);
        ExportNotificationCount = services.Count;
        IsExportNotificationOpen = true;
    }

    [RelayCommand]
    public void SetSortMode(ServiceSortMode mode) => SortMode = mode;

    [RelayCommand]
    public void SortAscending() => IsSortDescending = false;

    [RelayCommand]
    public void SortDescending() => IsSortDescending = true;

    [RelayCommand]
    public void SetGroupingMode(GroupingMode mode) => Grouping = mode;

    [RelayCommand]
    public async Task Refresh() => await RefreshDiscoveryAsync();

    [RelayCommand]
    public async Task StartDiscoveryAsync()
    {
        try
        {
            IsDiscovering = true;
            DiscoveryState = DiscoveryState.Discovering;
            StatusError = null;

            var options = new DiscoveryOptions
            {
                Mode = DiscoveryMode,
                Domain = "local",
                TargetServiceTypes = DiscoveryMode == DiscoveryMode.TargetedList
                    ? _settingsService.ReadSetting(SettingDefinitions.EnabledServiceTypes)
                    : null
            };

            await _engine.StartDiscoveryAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service discovery");
            DiscoveryState = DiscoveryState.Error;
            StatusError = ex.Message;
            IsDiscovering = false;
        }
    }

    [RelayCommand]
    public async Task StopDiscoveryAsync()
    {
        try
        {
            await _engine.StopDiscoveryAsync();
            IsDiscovering = false;
            DiscoveryState = DiscoveryState.Idle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop discovery");
        }
    }

    [RelayCommand]
    public async Task RefreshDiscoveryAsync()
    {
        _registry.Clear();
        SelectedService = null;

        await StopDiscoveryAsync();
        await StartDiscoveryAsync();
    }

    [RelayCommand]
    public async Task RestartDiscoveryAsync()
    {
        await StopDiscoveryAsync();
        await StartDiscoveryAsync();
    }

    private void OnEngineStateChanged(IDnsSdDiscoveryProvider? sender, DiscoveryStateChangedEventArgs e)
    {
        _dispatcher.Enqueue(() =>
        {
            IsDiscovering = e.NewState == DiscoveryState.Discovering;
            DiscoveryState = e.NewState;
        });
    }

    public void Dispose()
    {
        _engine.StateChanged -= OnEngineStateChanged;
        _registry.Dispose();
        GC.SuppressFinalize(this);
    }
}

