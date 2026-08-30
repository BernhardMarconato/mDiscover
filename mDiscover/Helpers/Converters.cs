using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using mDiscover.Core.Models;
using mDiscover.Strings;

namespace mDiscover.Helpers;

public partial class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolVal = value is bool b && b;
        if (Invert)
            boolVal = !boolVal;
        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var vis = value is Visibility v && v == Visibility.Visible;
        return Invert ? !vis : vis;
    }
}

public partial class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNotNull = value != null;
        if (value is string s)
            isNotNull = !string.IsNullOrWhiteSpace(s);
        if (Invert)
            isNotNull = !isNotNull;
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class CountToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = value is int c ? c : (value is System.Collections.ICollection col ? col.Count : 0);
        var hasItems = count > 0;
        if (Invert)
            hasItems = !hasItems;
        return hasItems ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b ? !b : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool b ? !b : false;
    }
}

public partial class ExportFormatToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ExportFormat format)
        {
            return ServiceDisplayFormatter.FormatInstanceCopyTooltip(format);
        }
        return ServiceDisplayFormatter.FormatInstanceCopyTooltip(ExportFormat.Markdown);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class ServiceCategoryToLocalizedStringConverter : IValueConverter
{
    public static string ConvertCategory(ServiceCategory category)
    {
        return category switch
        {
            ServiceCategory.WebAndApi => Resources.Category_WebAndApi,
            ServiceCategory.SmartHomeAndIot => Resources.Category_SmartHomeAndIot,
            ServiceCategory.MediaAndAudio => Resources.Category_MediaAndAudio,
            ServiceCategory.RemoteAccess => Resources.Category_RemoteAccess,
            ServiceCategory.Developer => Resources.Category_Developer,
            ServiceCategory.PrintingAndWorkshop => Resources.Category_PrintingAndWorkshop,
            ServiceCategory.CamerasAndVideo => Resources.Category_CamerasAndVideo,
            ServiceCategory.PrintAndScan => Resources.Category_PrintAndScan,
            ServiceCategory.StorageAndFiles => Resources.Category_StorageAndFiles,
            ServiceCategory.Infrastructure => Resources.Category_Infrastructure,
            ServiceCategory.Databases => Resources.Category_Databases,
            ServiceCategory.AppleEcosystem => Resources.Category_AppleEcosystem,
            _ => Resources.Category_OtherServices
        };
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ServiceCategory category)
        {
            return ConvertCategory(category);
        }
        return Resources.Category_OtherServices;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class ServiceTypeToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        ServiceCategory category;
        if (value is ServiceCategory cat)
        {
            category = cat;
        }
        else if (value is string serviceType)
        {
            category = WellKnownServiceCatalog.InferCategory(serviceType);
        }
        else
        {
            return FluentGlyphs.Network;
        }

        return ServiceDisplayFormatter.GetCategoryGlyph(category);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

/// <summary>
/// Provides localized display string formatting for service endpoints, hosts, resolution errors, and groupings.
/// </summary>
public static class ServiceDisplayFormatter
{
    public static bool Not(bool value) => !value;
    public static bool IsSortMode(ServiceSortMode current, ServiceSortMode target) => current == target;
    public static bool IsGroupingMode(GroupingMode current, GroupingMode target) => current == target;

    public static string FormatEndpoint(string? formattedEndpoint, ResolutionStatus status)
    {
        if (!string.IsNullOrWhiteSpace(formattedEndpoint))
            return formattedEndpoint;
        if (status == ResolutionStatus.Resolving)
            return Resources.Inspector_ResolvingSubtitle;
        if (status == ResolutionStatus.Failed)
            return Resources.Inspector_ResolutionFailed;
        return Resources.Inspector_UnspecifiedEndpoint;
    }

    public static string FormatCardSubtitle(string? serviceType, string? formattedEndpoint, ResolutionStatus status)
    {
        var ep = FormatEndpoint(formattedEndpoint, status);
        if (!string.IsNullOrWhiteSpace(serviceType))
        {
            return $"{serviceType} • {ep}";
        }
        return ep;
    }

    public static string FormatHost(string? hostName, ResolutionStatus status)
    {
        if (!string.IsNullOrWhiteSpace(hostName))
            return hostName;
        if (status == ResolutionStatus.Resolving)
            return Resources.Inspector_ResolvingHost;
        if (status == ResolutionStatus.Failed)
            return Resources.Inspector_HostUnavailable;
        return Resources.Inspector_UnspecifiedHost;
    }

    public static string FormatResolutionError(ResolutionFailureReason reason, string? hostName, string? details)
    {
        return reason switch
        {
            ResolutionFailureReason.NoHostName => Resources.ResolutionError_NoHostName,
            ResolutionFailureReason.NoSrvRecords => Resources.ResolutionError_NoSrvRecords,
            ResolutionFailureReason.NoAddressesFound => !string.IsNullOrWhiteSpace(hostName)
                ? Resources.ResolutionError_NoAddressesFound(hostName)
                : Resources.ResolutionError_Generic,
            ResolutionFailureReason.DnsQueryFailed => !string.IsNullOrWhiteSpace(details)
                ? Resources.ResolutionError_DnsQueryFailed(details)
                : Resources.ResolutionError_Generic,
            ResolutionFailureReason.Timeout => Resources.ResolutionError_Timeout,
            _ => !string.IsNullOrWhiteSpace(details) ? details : Resources.ResolutionError_Generic
        };
    }

    public static string GetProviderDisplayName(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return string.Empty;
        if (providerId.Equals("winrt", StringComparison.OrdinalIgnoreCase))
            return Resources.Provider_WinRt;
        if (providerId.Equals("fake", StringComparison.OrdinalIgnoreCase))
            return Resources.Provider_Fake;
        return Resources.Provider_Win32;
    }

    public static string FormatGroupTitle(string key, GroupingMode mode)
    {
        if (mode == GroupingMode.ByServiceType)
        {
            var def = WellKnownServiceCatalog.GetOrInfer(key);
            return def.DisplayName;
        }

        return string.IsNullOrWhiteSpace(key) ? Resources.Grouping_UnknownHost : key;
    }

    public static string FormatGroupSubtitle(string key, GroupingMode mode)
    {
        if (mode == GroupingMode.ByServiceType)
        {
            return key;
        }

        return Resources.Grouping_HostSubtitle;
    }

    public static string GetCategoryGlyph(ServiceCategory category)
    {
        return category switch
        {
            ServiceCategory.WebAndApi => FluentGlyphs.WebGlobe,
            ServiceCategory.RemoteAccess => FluentGlyphs.RemoteAccess,
            ServiceCategory.MediaAndAudio => FluentGlyphs.TvSpeaker,
            ServiceCategory.SmartHomeAndIot => FluentGlyphs.Lightbulb,
            ServiceCategory.PrintAndScan => FluentGlyphs.Printer,
            ServiceCategory.StorageAndFiles => FluentGlyphs.Storage,
            ServiceCategory.PrintingAndWorkshop => FluentGlyphs.Printer,
            ServiceCategory.CamerasAndVideo => FluentGlyphs.Camera,
            ServiceCategory.AppleEcosystem => FluentGlyphs.ConnectedDevices,
            ServiceCategory.Databases => FluentGlyphs.Database,
            ServiceCategory.Developer => FluentGlyphs.DeveloperCode,
            ServiceCategory.Infrastructure => FluentGlyphs.Network,
            _ => FluentGlyphs.Network
        };
    }

    private static readonly (string[] Keywords, string Glyph)[] _hostGlyphKeywordRules =
    [
        (["printer", "print", "laserjet", "deskjet", "epson", "canon", "brother"], FluentGlyphs.Printer),
        (["light", "bulb", "hue", "wled", "shelly", "yeelight", "elgato", "lamp"], FluentGlyphs.Lightbulb),
        (["tv", "chromecast", "cast", "waipu", "firetv", "sonos", "speaker", "audio", "sound"], FluentGlyphs.TvSpeaker),
        (["ipad", "iphone", "phone", "android", "pixel", "galaxy"], FluentGlyphs.MobileTablet),
        (["laptop", "macbook", "notebook"], FluentGlyphs.Laptop),
        (["pi", "raspberry", "nas", "server", "synology", "qnap", "gateway", "router", "desktop", "pc", "station"], FluentGlyphs.ServerHost)
    ];

    public static string FormatGroupGlyph(string key, GroupingMode mode)
    {
        if (mode == GroupingMode.ByServiceType)
        {
            var category = WellKnownServiceCatalog.InferCategory(key);
            return GetCategoryGlyph(category);
        }

        if (string.IsNullOrWhiteSpace(key))
            return FluentGlyphs.ServerHost;

        foreach (var (keywords, glyph) in _hostGlyphKeywordRules)
        {
            if (keywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                return glyph;
            }
        }

        return FluentGlyphs.ServerHost;
    }

    public static string GetFormatDisplayName(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Markdown => Resources.Format_Markdown,
            ExportFormat.Json => Resources.Format_Json,
            ExportFormat.Csv => Resources.Format_Csv,
            ExportFormat.Text => Resources.Format_Text,
            _ => Resources.Format_Markdown
        };
    }

    public static string FormatExportNotification(int count, ExportFormat format)
    {
        return Resources.Export_Notification(count, GetFormatDisplayName(format));
    }

    public static string FormatExportTooltip(ExportFormat format)
    {
        return Resources.Export_AllAsFormat(GetFormatDisplayName(format));
    }

    public static string FormatInstanceCopyTooltip(ExportFormat format)
    {
        return Resources.Copy_AsFormat(GetFormatDisplayName(format));
    }

    public static string FormatServiceStatusBadgeText(bool isOnline, ResolutionStatus status)
    {
        if (!isOnline)
        {
            return Resources.Inspector_Offline;
        }

        if (status == ResolutionStatus.Failed)
        {
            return Resources.Inspector_Unreachable;
        }

        if (status == ResolutionStatus.Resolving)
        {
            return Resources.Inspector_ResolvingSubtitle;
        }

        return Resources.Inspector_Online;
    }

    private static Brush GetThemeBrush(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var val) && val is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public static Brush GetServiceStatusBadgeBackground(bool isOnline, ResolutionStatus status)
    {
        var key = !isOnline || status == ResolutionStatus.Resolving
            ? "SystemFillColorNeutralBackgroundBrush"
            : status == ResolutionStatus.Failed
                ? "SystemFillColorCautionBackgroundBrush"
                : "SystemFillColorSuccessBackgroundBrush";

        return GetThemeBrush(key);
    }

    public static Brush GetServiceStatusBadgeForeground(bool isOnline, ResolutionStatus status)
    {
        var key = !isOnline || status == ResolutionStatus.Resolving
            ? "TextFillColorSecondaryBrush"
            : "TextFillColorPrimaryBrush";

        return GetThemeBrush(key);
    }

    public static Brush GetServiceStatusDotBrush(bool isOnline, ResolutionStatus status)
    {
        var key = !isOnline || status == ResolutionStatus.Resolving
            ? "SystemFillColorNeutralBrush"
            : status == ResolutionStatus.Failed
                ? "SystemFillColorCautionBrush"
                : "SystemFillColorSuccessBrush";

        return GetThemeBrush(key);
    }

    public static string GetServiceStatusTooltip(bool isOnline, ResolutionStatus status)
    {
        return FormatServiceStatusBadgeText(isOnline, status);
    }

    public static string FormatTimestamp(DateTimeOffset timestamp)
    {
        var local = timestamp.ToLocalTime();
        return local.ToString("g");
    }

    public static string FormatTimeDetailed(DateTimeOffset timestamp)
    {
        var local = timestamp.ToLocalTime();
        return local.ToString("G");
    }
}


