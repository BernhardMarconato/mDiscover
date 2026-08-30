using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mDiscover.Core.Common;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;

namespace mDiscover.ViewModels;

/// <summary>
/// Manages application configuration, theme preferences, provider selection, custom DNS-SD service types, and diagnostic tools.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IServiceDiscoveryEngine _engine;
    private readonly Func<Task> _onConfigChanged;
    private readonly IUriLauncherService _launcherService;
    private readonly IAppLifecycleService _lifecycleService;
    private readonly IAppPathService _appPathService;
    private readonly AppTheme _initialTheme;

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    [ObservableProperty]
    public partial double SidebarWidth { get; set; } = 360.0;

    partial void OnSidebarWidthChanged(double value)
    {
        if (value >= 200 && value <= 800)
        {
            _settingsService.SaveSetting(SettingDefinitions.SidebarWidth, value);
        }
    }

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; }

    [ObservableProperty]
    public partial bool IsRestartBannerVisible { get; set; }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Default
        };

        _settingsService.SaveSetting(SettingDefinitions.AppTheme, theme);
        IsRestartBannerVisible = theme != _initialTheme;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWildcardSupported))]
    [NotifyPropertyChangedFor(nameof(ActiveProviderIndex))]
    public partial IDnsSdDiscoveryProvider ActiveProvider { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWildcardMode))]
    [NotifyPropertyChangedFor(nameof(IsTargetedMode))]
    [NotifyPropertyChangedFor(nameof(IsHybridMode))]
    [NotifyPropertyChangedFor(nameof(DiscoveryModeIndex))]
    public partial DiscoveryMode DiscoveryMode { get; set; } = DiscoveryMode.WildcardMeta;

    public bool IsWildcardSupported => ActiveProvider?.SupportsWildcardDiscovery ?? false;

    public bool IsWildcardMode
    {
        get => DiscoveryMode == DiscoveryMode.WildcardMeta;
        set
        {
            if (value && DiscoveryMode != DiscoveryMode.WildcardMeta)
            {
                DiscoveryMode = DiscoveryMode.WildcardMeta;
                _settingsService.SaveSetting(SettingDefinitions.DiscoveryMode, DiscoveryMode.WildcardMeta);
                _ = _onConfigChanged?.Invoke();
            }
        }
    }

    public bool IsTargetedMode
    {
        get => DiscoveryMode == DiscoveryMode.TargetedList;
        set
        {
            if (value && DiscoveryMode != DiscoveryMode.TargetedList)
            {
                DiscoveryMode = DiscoveryMode.TargetedList;
                _settingsService.SaveSetting(SettingDefinitions.DiscoveryMode, DiscoveryMode.TargetedList);
                _ = _onConfigChanged?.Invoke();
            }
        }
    }

    public bool IsHybridMode
    {
        get => DiscoveryMode == DiscoveryMode.Hybrid;
        set
        {
            if (value && DiscoveryMode != DiscoveryMode.Hybrid)
            {
                DiscoveryMode = DiscoveryMode.Hybrid;
                _settingsService.SaveSetting(SettingDefinitions.DiscoveryMode, DiscoveryMode.Hybrid);
                _ = _onConfigChanged?.Invoke();
            }
        }
    }

    public int DiscoveryModeIndex
    {
        get => DiscoveryMode == DiscoveryMode.WildcardMeta ? 0 : 1;
        set
        {
            var mode = value == 0 ? DiscoveryMode.WildcardMeta : DiscoveryMode.TargetedList;
            if (DiscoveryMode != mode)
            {
                DiscoveryMode = mode;
                _settingsService.SaveSetting(SettingDefinitions.DiscoveryMode, mode);
                _ = _onConfigChanged?.Invoke();
            }
        }
    }

    public int ActiveProviderIndex
    {
        get
        {
            var providers = _engine.AvailableProviders;
            for (var i = 0; i < providers.Count; i++)
            {
                if (providers[i] == ActiveProvider)
                    return i;
            }
            return 0;
        }
        set
        {
            var providers = _engine.AvailableProviders;
            if (value >= 0 && value < providers.Count)
            {
                _ = ChangeProviderAsync(providers[value]);
            }
        }
    }

    public async Task ChangeProviderAsync(IDnsSdDiscoveryProvider targetProvider)
    {
        if (ActiveProvider == targetProvider)
            return;

        await _engine.SetActiveProviderAsync(targetProvider);
        ActiveProvider = _engine.ActiveProvider;

        if (!ActiveProvider.SupportsWildcardDiscovery && DiscoveryMode == DiscoveryMode.WildcardMeta)
        {
            DiscoveryMode = DiscoveryMode.TargetedList;
            _settingsService.SaveSetting(SettingDefinitions.DiscoveryMode, DiscoveryMode.TargetedList);
        }

        await _onConfigChanged();
    }

    [ObservableProperty]
    public partial string CustomServiceTypeInput { get; set; } = string.Empty;

    public ObservableCollection<ServiceTypeConfigViewModel> BuiltInServiceTypes { get; } = [];
    public ObservableCollection<ServiceTypeConfigViewModel> CustomServiceTypes { get; } = [];
    public ObservableCollection<ServiceTypeConfigViewModel> ConfigurableServiceTypes { get; } = [];
    public string AppVersion => AppInfo.DisplayVersionString;
    public string ShortAppVersion => AppInfo.VersionString;
    public string? CommitHash => AppInfo.CommitHash;

    public SettingsViewModel(
        ISettingsService settingsService,
        IServiceDiscoveryEngine engine,
        Func<Task> onConfigChanged,
        IUriLauncherService launcherService,
        IAppLifecycleService lifecycleService,
        IAppPathService appPathService)
    {
        _settingsService = settingsService;
        _engine = engine;
        _onConfigChanged = onConfigChanged;
        _launcherService = launcherService;
        _lifecycleService = lifecycleService;
        _appPathService = appPathService;

        SidebarWidth = _settingsService.ReadSetting(SettingDefinitions.SidebarWidth);
        DiscoveryMode = _settingsService.ReadSetting(SettingDefinitions.DiscoveryMode);
        ActiveProvider = engine.ActiveProvider;

        _initialTheme = _settingsService.ReadSetting(SettingDefinitions.AppTheme);
        SelectedThemeIndex = _initialTheme switch
        {
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };

        LoadServiceTypeConfigs();
    }

    [RelayCommand]
    public void RestartApp()
    {
        _lifecycleService.Restart();
    }

    [RelayCommand]
    public void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    [RelayCommand]
    public void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    public void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    public async Task OpenLicensesFolderAsync()
    {
        var licensesPath = AppInfo.GetLicensesFolderPath();
        if (licensesPath != null)
        {
            await _launcherService.LaunchFolderPathAsync(licensesPath);
        }
    }

    [RelayCommand]
    public async Task OpenLogFolderAsync()
    {
        var logsDir = _appPathService.LogFolderPath;
        if (Directory.Exists(logsDir))
        {
            await _launcherService.LaunchFolderPathAsync(logsDir);
        }
    }

    [RelayCommand]
    public async Task ResetCustomServiceTypesAsync()
    {
        _settingsService.SaveSetting(SettingDefinitions.CustomServiceTypes, new List<string>());
        LoadServiceTypeConfigs();
        await _onConfigChanged();
    }

    [RelayCommand]
    public async Task AddCustomServiceTypeAsync()
    {
        var serviceType = AppInfo.NormalizeServiceType(CustomServiceTypeInput);
        if (string.IsNullOrWhiteSpace(serviceType))
            return;

        var existingCustom = _settingsService.ReadSetting(SettingDefinitions.CustomServiceTypes).ToList();
        if (!existingCustom.Contains(serviceType, StringComparer.OrdinalIgnoreCase))
        {
            existingCustom.Add(serviceType);
            _settingsService.SaveSetting(SettingDefinitions.CustomServiceTypes, existingCustom);

            var enabled = _settingsService.ReadSetting(SettingDefinitions.EnabledServiceTypes).ToList();
            if (!enabled.Contains(serviceType, StringComparer.OrdinalIgnoreCase))
            {
                enabled.Add(serviceType);
                _settingsService.SaveSetting(SettingDefinitions.EnabledServiceTypes, enabled);
            }

            CustomServiceTypeInput = string.Empty;
            LoadServiceTypeConfigs();
            await _onConfigChanged();
        }
    }

    public async Task RemoveCustomServiceTypeAsync(string serviceType)
    {
        var existingCustom = _settingsService.ReadSetting(SettingDefinitions.CustomServiceTypes).ToList();
        if (existingCustom.RemoveAll(t => t.Equals(serviceType, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            _settingsService.SaveSetting(SettingDefinitions.CustomServiceTypes, existingCustom);

            var enabled = _settingsService.ReadSetting(SettingDefinitions.EnabledServiceTypes).ToList();
            enabled.RemoveAll(t => t.Equals(serviceType, StringComparison.OrdinalIgnoreCase));
            _settingsService.SaveSetting(SettingDefinitions.EnabledServiceTypes, enabled);

            LoadServiceTypeConfigs();
            await _onConfigChanged();
        }
    }

    [RelayCommand]
    public async Task SelectAllServiceTypesAsync()
    {
        var all = ConfigurableServiceTypes.Select(t => t.ServiceType).ToList();
        _settingsService.SaveSetting(SettingDefinitions.EnabledServiceTypes, all);
        foreach (var t in ConfigurableServiceTypes)
            t.IsEnabled = true;

        await _onConfigChanged();
    }

    [RelayCommand]
    public async Task DeselectAllServiceTypesAsync()
    {
        _settingsService.SaveSetting(SettingDefinitions.EnabledServiceTypes, new List<string>());
        foreach (var t in ConfigurableServiceTypes)
            t.IsEnabled = false;

        await _onConfigChanged();
    }

    private void LoadServiceTypeConfigs()
    {
        BuiltInServiceTypes.Clear();
        CustomServiceTypes.Clear();
        ConfigurableServiceTypes.Clear();

        var enabledTypes = _settingsService.ReadSetting(SettingDefinitions.EnabledServiceTypes).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var customTypes = _settingsService.ReadSetting(SettingDefinitions.CustomServiceTypes).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var def in WellKnownServiceCatalog.All)
        {
            var isCustom = customTypes.Contains(def.ServiceType);
            var isEnabled = enabledTypes.Count == 0 || enabledTypes.Contains(def.ServiceType);

            var vm = new ServiceTypeConfigViewModel(
                def.ServiceType,
                isEnabled,
                isCustom,
                onToggled: OnServiceTypeToggled,
                onRemove: async (st) => await RemoveCustomServiceTypeAsync(st));

            if (isCustom)
            {
                CustomServiceTypes.Add(vm);
            }
            else
            {
                BuiltInServiceTypes.Add(vm);
            }

            ConfigurableServiceTypes.Add(vm);
        }

        foreach (var custom in customTypes)
        {
            if (ConfigurableServiceTypes.Any(t => t.ServiceType.Equals(custom, StringComparison.OrdinalIgnoreCase)))
                continue;

            var isEnabled = enabledTypes.Contains(custom);
            var vm = new ServiceTypeConfigViewModel(
                custom,
                isEnabled,
                isCustom: true,
                onToggled: OnServiceTypeToggled,
                onRemove: async (st) => await RemoveCustomServiceTypeAsync(st));

            CustomServiceTypes.Add(vm);
            ConfigurableServiceTypes.Add(vm);
        }
    }

    private void OnServiceTypeToggled()
    {
        var enabled = ConfigurableServiceTypes
            .Where(t => t.IsEnabled)
            .Select(t => t.ServiceType)
            .ToList();

        _settingsService.SaveSetting(SettingDefinitions.EnabledServiceTypes, enabled);
        _onConfigChanged.Invoke();
    }
}

