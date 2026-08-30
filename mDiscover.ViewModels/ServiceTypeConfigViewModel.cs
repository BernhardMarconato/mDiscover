using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mDiscover.Core.Models;

namespace mDiscover.ViewModels;

/// <summary>
/// Represents a configurable DNS-SD service type item in the Settings catalog list,
/// supporting enable/disable toggling and removal of custom user-defined types.
/// </summary>
public partial class ServiceTypeConfigViewModel : ObservableObject
{
    private readonly Action? _onToggled;
    private readonly Action<string>? _onRemove;

    [ObservableProperty]
    public partial string ServiceType { get; set; }

    [ObservableProperty]
    public partial string DisplayName { get; set; }

    [ObservableProperty]
    public partial ServiceCategory Category { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsCustom { get; set; }

    partial void OnIsEnabledChanged(bool value)
    {
        _onToggled?.Invoke();
    }

    public ServiceTypeConfigViewModel(
        string serviceType,
        bool isEnabled,
        bool isCustom,
        Action? onToggled = null,
        Action<string>? onRemove = null)
    {
        ServiceType = serviceType;
        IsEnabled = isEnabled;
        IsCustom = isCustom;
        _onToggled = onToggled;
        _onRemove = onRemove;

        var def = WellKnownServiceCatalog.GetOrInfer(serviceType);
        DisplayName = def.DisplayName;
        Category = def.Category;
    }

    [RelayCommand]
    public void Remove()
    {
        _onRemove?.Invoke(ServiceType);
    }
}

