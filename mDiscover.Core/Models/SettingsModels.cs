namespace mDiscover.Core.Models;

/// <summary>
/// Defines application UI theme options.
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Follows the Windows system theme setting.
    /// </summary>
    Default,

    /// <summary>
    /// Forces Fluent Light theme.
    /// </summary>
    Light,

    /// <summary>
    /// Forces Fluent Dark theme.
    /// </summary>
    Dark
}

/// <summary>
/// Defines grouping modes for discovered services in the presentation view.
/// </summary>
public enum GroupingMode
{
    /// <summary>
    /// Groups services by their DNS-SD service type (e.g. HTTP Servers, Printers, Smart Home).
    /// </summary>
    ByServiceType,

    /// <summary>
    /// Groups services by their host name or primary IP address.
    /// </summary>
    ByHost
}

/// <summary>
/// Defines sorting criteria for discovered services.
/// </summary>
public enum ServiceSortMode
{
    /// <summary>
    /// Sorts alphabetically by service instance name.
    /// </summary>
    Name,

    /// <summary>
    /// Sorts by primary IP address.
    /// </summary>
    IpAddress,

    /// <summary>
    /// Sorts numerically by network port.
    /// </summary>
    Port,

    /// <summary>
    /// Sorts chronologically by discovery timestamp.
    /// </summary>
    RecentlyDiscovered
}

/// <summary>
/// Supported data export serialization formats.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Human-readable plain text blocks.
    /// </summary>
    Text,

    /// <summary>
    /// GitHub-flavored Markdown document with tables.
    /// </summary>
    Markdown,

    /// <summary>
    /// Comma-Separated Values table.
    /// </summary>
    Csv,

    /// <summary>
    /// Structured JSON payload.
    /// </summary>
    Json
}

/// <summary>
/// Strongly-typed setting key and default value pair.
/// </summary>
/// <typeparam name="T">The type of the setting value.</typeparam>
/// <param name="Key">The unique storage key identifier.</param>
/// <param name="DefaultValue">The default value used when the setting is not yet persisted.</param>
public record SettingDefinition<T>(string Key, T DefaultValue);

/// <summary>
/// Application setting definitions and defaults.
/// </summary>
public static class SettingDefinitions
{
    /// <summary>Application UI theme preference.</summary>
    public static readonly SettingDefinition<AppTheme> AppTheme = new("AppTheme", Models.AppTheme.Default);

    /// <summary>Sidebar navigation panel width in DIPs.</summary>
    public static readonly SettingDefinition<double> SidebarWidth = new("SidebarWidth", 360.0);

    /// <summary>Default data export serialization format.</summary>
    public static readonly SettingDefinition<ExportFormat> DefaultExportFormat = new("DefaultExportFormat", ExportFormat.Markdown);

    /// <summary>Service list grouping mode.</summary>
    public static readonly SettingDefinition<GroupingMode> GroupingMode = new("GroupingMode", Models.GroupingMode.ByHost);

    /// <summary>Service list sort mode.</summary>
    public static readonly SettingDefinition<ServiceSortMode> SortMode = new("SortMode", ServiceSortMode.Name);

    /// <summary>Whether service sorting is descending.</summary>
    public static readonly SettingDefinition<bool> IsSortDescending = new("IsSortDescending", false);

    /// <summary>DNS-SD discovery strategy mode.</summary>
    public static readonly SettingDefinition<DiscoveryMode> DiscoveryMode = new("DiscoveryMode", Models.DiscoveryMode.WildcardMeta);

    /// <summary>Active/preferred DNS-SD provider identifier.</summary>
    public static readonly SettingDefinition<string> PreferredProvider = new("PreferredProvider", "win32");

    /// <summary>Custom targeted service types enabled for scanning.</summary>
    public static readonly SettingDefinition<List<string>> EnabledServiceTypes = new("EnabledServiceTypes", []);

    /// <summary>User-defined custom service types.</summary>
    public static readonly SettingDefinition<List<string>> CustomServiceTypes = new("CustomServiceTypes", []);

    /// <summary>Saved window position and geometry.</summary>
    public static readonly SettingDefinition<string?> WindowPlacement = new("WindowPlacement", null);
}

