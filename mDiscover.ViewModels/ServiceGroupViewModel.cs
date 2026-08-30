using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using mDiscover.Core.Models;

namespace mDiscover.ViewModels;

/// <summary>
/// Represents a collapsible grouping node (by host/device or by service type) in the sidebar discovery tree.
/// </summary>
public partial class ServiceGroupViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Key { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ServiceCategory? Category { get; set; }

    [ObservableProperty]
    public partial GroupingMode Mode { get; set; } = GroupingMode.ByServiceType;

    public ObservableCollection<DiscoveredServiceViewModel> Services { get; } = [];

    public int Count => Services.Count;
    public bool IsEmpty => Services.Count == 0;

    public ServiceGroupViewModel(string key, GroupingMode mode)
    {
        Key = key;
        Mode = mode;
        UpdateHeaderInfo();
    }

    public void UpdateHeaderInfo()
    {
        if (Mode == GroupingMode.ByServiceType)
        {
            var def = WellKnownServiceCatalog.GetOrInfer(Key);
            Title = def.DisplayName;
            Subtitle = Key;
            Category = def.Category;
        }
        else
        {
            var (title, subtitle) = ResolveHostHeaderInfo();
            Title = title;
            Subtitle = subtitle;
            Category = null;
        }

        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public void NotifyCountChanged() => UpdateHeaderInfo();

    private (string Title, string Subtitle) ResolveHostHeaderInfo()
    {
        var friendlyName = ExtractFriendlyDeviceName();
        var primaryIp = Services.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.PrimaryIp))?.PrimaryIp;
        var hostName = Services.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.HostName))?.HostName;

        var hostDisplay = hostName ?? Key;
        var title = !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : hostDisplay;

        var subtitle = !string.IsNullOrWhiteSpace(primaryIp) && !hostDisplay.Equals(primaryIp, StringComparison.OrdinalIgnoreCase)
            ? $"{hostDisplay} • {primaryIp}"
            : hostDisplay;

        return (title, subtitle);
    }

    private string? ExtractFriendlyDeviceName()
    {
        foreach (var s in Services)
        {
            var fn = s.Model.TxtRecords.FirstOrDefault(r => r.Key.Equals("fn", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(fn))
                return fn;

            var md = s.Model.TxtRecords.FirstOrDefault(r => r.Key.Equals("md", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrWhiteSpace(md))
                return md;
        }

        if (Services.Count == 0)
            return null;

        var candidate = Services.FirstOrDefault(s => !IsHexOrGuid(s.InstanceName))?.InstanceName
                        ?? Services[0].InstanceName;

        if (candidate.Contains('@'))
        {
            var parts = candidate.Split('@', StringSplitOptions.TrimEntries);
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                return char.ToUpperInvariant(parts[1][0]) + parts[1][1..];
            }
            return parts[0];
        }

        return !IsHexOrGuid(candidate) ? candidate : null;
    }

    private static bool IsHexOrGuid(string str)
    {
        if (Guid.TryParse(str, out _))
            return true;

        return str.Length >= 12 && str.All(c => char.IsAsciiHexDigit(c) || c == '-' || c == ':');
    }
}

