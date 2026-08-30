using System.Collections.ObjectModel;
using mDiscover.Core.Collections;
using mDiscover.Core.Models;

namespace mDiscover.ViewModels.Services;

/// <summary>
/// Provides pure algorithmic operations for filtering, sorting, grouping, and delta-synchronizing discovered services.
/// </summary>
public static class ServiceGroupingPipeline
{
    /// <summary>
    /// Filters a sequence of discovered services by search text and optional category.
    /// </summary>
    public static IEnumerable<DiscoveredServiceViewModel> Filter(
        IEnumerable<DiscoveredServiceViewModel> services,
        string? searchText,
        ServiceCategory? category)
    {
        var query = services;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(s =>
                s.InstanceName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.ServiceType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.HostName != null && s.HostName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                s.PrimaryIp.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.TxtRecords.Any(t => t.Key.Contains(term, StringComparison.OrdinalIgnoreCase) || t.Value.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (category.HasValue)
        {
            query = query.Where(s => s.Category == category.Value);
        }

        return query;
    }

    /// <summary>
    /// Gets a typed comparer for discovered services based on the specified sort mode and direction.
    /// </summary>
    public static IComparer<DiscoveredServiceViewModel> GetServicesComparer(ServiceSortMode sortMode, bool isDescending)
    {
        return (sortMode, isDescending) switch
        {
            (ServiceSortMode.Name, false) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => string.Compare(a.InstanceName, b.InstanceName, StringComparison.OrdinalIgnoreCase)),
            (ServiceSortMode.Name, true) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => string.Compare(b.InstanceName, a.InstanceName, StringComparison.OrdinalIgnoreCase)),
            (ServiceSortMode.IpAddress, false) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => string.Compare(a.PrimaryIp, b.PrimaryIp, StringComparison.OrdinalIgnoreCase)),
            (ServiceSortMode.IpAddress, true) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => string.Compare(b.PrimaryIp, a.PrimaryIp, StringComparison.OrdinalIgnoreCase)),
            (ServiceSortMode.Port, false) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => (a.Port ?? int.MaxValue).CompareTo(b.Port ?? int.MaxValue)),
            (ServiceSortMode.Port, true) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => (b.Port ?? int.MinValue).CompareTo(a.Port ?? int.MinValue)),
            (ServiceSortMode.RecentlyDiscovered, false) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => a.LastSeen.CompareTo(b.LastSeen)),
            (ServiceSortMode.RecentlyDiscovered, true) => Comparer<DiscoveredServiceViewModel>.Create((a, b) => b.LastSeen.CompareTo(a.LastSeen)),
            _ => Comparer<DiscoveredServiceViewModel>.Create((a, b) => string.Compare(a.InstanceName, b.InstanceName, StringComparison.OrdinalIgnoreCase))
        };
    }

    /// <summary>
    /// Generates a group key for device-based host grouping.
    /// </summary>
    public static string GetDeviceGroupKey(DiscoveredServiceViewModel s)
    {
        if (!string.IsNullOrWhiteSpace(s.PrimaryIp))
            return s.PrimaryIp;

        if (!string.IsNullOrWhiteSpace(s.HostName))
            return s.HostName;

        return s.FullServicePath;
    }

    /// <summary>
    /// Gets a user-friendly display name for a service group.
    /// </summary>
    public static string GetGroupDisplayName(IGrouping<string, DiscoveredServiceViewModel> group, GroupingMode mode)
    {
        if (mode == GroupingMode.ByServiceType)
        {
            return WellKnownServiceCatalog.GetOrInfer(group.Key).DisplayName;
        }

        var candidate = group.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.InstanceName))?.InstanceName;
        return !string.IsNullOrWhiteSpace(candidate) ? candidate : group.Key;
    }

    /// <summary>
    /// Orders grouped services according to the sort mode, direction, and grouping mode.
    /// </summary>
    public static IEnumerable<IGrouping<string, DiscoveredServiceViewModel>> OrderGroups(
        IEnumerable<IGrouping<string, DiscoveredServiceViewModel>> groups,
        ServiceSortMode sortMode,
        bool isDescending,
        GroupingMode groupingMode)
    {
        return (sortMode, isDescending) switch
        {
            (ServiceSortMode.Name, false) => groups.OrderBy(g => GetGroupDisplayName(g, groupingMode), StringComparer.OrdinalIgnoreCase),
            (ServiceSortMode.Name, true) => groups.OrderByDescending(g => GetGroupDisplayName(g, groupingMode), StringComparer.OrdinalIgnoreCase),
            (ServiceSortMode.RecentlyDiscovered, false) => groups.OrderBy(g => g.Min(s => s.LastSeen)),
            (ServiceSortMode.RecentlyDiscovered, true) => groups.OrderByDescending(g => g.Max(s => s.LastSeen)),
            (ServiceSortMode.IpAddress, false) => groups.OrderBy(g => g.FirstOrDefault()?.PrimaryIp ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            (ServiceSortMode.IpAddress, true) => groups.OrderByDescending(g => g.FirstOrDefault()?.PrimaryIp ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            (ServiceSortMode.Port, false) => groups.OrderBy(g => g.Min(s => s.Port ?? int.MaxValue)),
            (ServiceSortMode.Port, true) => groups.OrderByDescending(g => g.Min(s => s.Port ?? int.MaxValue)),
            (_, false) => groups.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase),
            (_, true) => groups.OrderByDescending(g => g.Key, StringComparer.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// Synchronizes target group collection and filtered collection with minimal UI collection mutations.
    /// </summary>
    public static void SyncCollections(
        ObservableCollection<ServiceGroupViewModel> targetGroups,
        ObservableCollection<DiscoveredServiceViewModel> targetFiltered,
        IReadOnlyList<DiscoveredServiceViewModel> filteredList,
        GroupingMode groupingMode,
        ServiceSortMode sortMode,
        bool isDescending)
    {
        var comparer = GetServicesComparer(sortMode, isDescending);

        IEnumerable<IGrouping<string, DiscoveredServiceViewModel>> rawGroups = groupingMode == GroupingMode.ByServiceType
            ? filteredList.GroupBy(s => s.ServiceType, StringComparer.OrdinalIgnoreCase)
            : filteredList.GroupBy(GetDeviceGroupKey, StringComparer.OrdinalIgnoreCase);

        var targetGroupSpecs = OrderGroups(rawGroups, sortMode, isDescending, groupingMode).ToList();
        var targetGroupKeys = new HashSet<string>(targetGroupSpecs.Select(g => g.Key), StringComparer.OrdinalIgnoreCase);

        // Remove groups no longer present or with differing grouping modes
        for (var i = targetGroups.Count - 1; i >= 0; i--)
        {
            var group = targetGroups[i];
            if (group.Mode != groupingMode || !targetGroupKeys.Contains(group.Key))
            {
                targetGroups.RemoveAt(i);
            }
        }

        // Add or sync groups in order with minimal delta mutations
        for (var i = 0; i < targetGroupSpecs.Count; i++)
        {
            var spec = targetGroupSpecs[i];
            var orderedItems = spec.Order(comparer).ToList();

            var existingIndex = -1;
            for (var j = 0; j < targetGroups.Count; j++)
            {
                if (targetGroups[j].Mode == groupingMode && targetGroups[j].Key.Equals(spec.Key, StringComparison.OrdinalIgnoreCase))
                {
                    existingIndex = j;
                    break;
                }
            }

            ServiceGroupViewModel group;
            if (existingIndex == -1)
            {
                group = new ServiceGroupViewModel(spec.Key, groupingMode);
                group.Services.SyncTo(orderedItems);
                group.UpdateHeaderInfo();
                targetGroups.Insert(i, group);
            }
            else
            {
                group = targetGroups[existingIndex];
                if (existingIndex != i)
                {
                    targetGroups.Move(existingIndex, i);
                }

                if (group.Services.SyncTo(orderedItems))
                {
                    group.NotifyCountChanged();
                }
                else
                {
                    group.UpdateHeaderInfo();
                }
            }
        }

        var orderedFiltered = filteredList.Order(comparer).ToList();
        targetFiltered.SyncTo(orderedFiltered);
    }
}
