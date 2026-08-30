using mDiscover.Core.Models;

namespace mDiscover.Core.Interfaces;

/// <summary>
/// Defines multi-format serialization contracts for discovered DNS-SD services.
/// Supports JSON, Markdown, CSV, and formatted plain text output.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Serializes a collection of discovered services into the specified export format.
    /// </summary>
    /// <param name="services">The services to export.</param>
    /// <param name="format">The target export serialization format.</param>
    /// <returns>The formatted output string.</returns>
    string Export(IEnumerable<DiscoveredService> services, ExportFormat format);

    /// <summary>
    /// Serializes a single discovered service into the specified export format.
    /// </summary>
    /// <param name="service">The service to export.</param>
    /// <param name="format">The target export serialization format.</param>
    /// <returns>The formatted output string.</returns>
    string Export(DiscoveredService service, ExportFormat format);

    /// <summary>
    /// Formats a collection of discovered services as structured JSON.
    /// </summary>
    /// <param name="services">The services to serialize.</param>
    /// <returns>A formatted JSON string.</returns>
    string ToJson(IEnumerable<DiscoveredService> services);

    /// <summary>
    /// Formats a single discovered service as structured JSON.
    /// </summary>
    /// <param name="service">The service to serialize.</param>
    /// <returns>A formatted JSON string.</returns>
    string ToJson(DiscoveredService service);

    /// <summary>
    /// Formats a collection of discovered services as human-readable plain text blocks.
    /// </summary>
    /// <param name="services">The services to format.</param>
    /// <returns>A plain text representation of the services.</returns>
    string ToPlainText(IEnumerable<DiscoveredService> services);

    /// <summary>
    /// Formats a single discovered service as human-readable plain text.
    /// </summary>
    /// <param name="service">The service to format.</param>
    /// <returns>A plain text representation of the service.</returns>
    string ToPlainText(DiscoveredService service);

    /// <summary>
    /// Formats a collection of discovered services as GitHub-flavored Markdown document with tables.
    /// </summary>
    /// <param name="services">The services to format.</param>
    /// <returns>A markdown formatted string.</returns>
    string ToMarkdown(IEnumerable<DiscoveredService> services);

    /// <summary>
    /// Formats a single discovered service as a detailed Markdown document.
    /// </summary>
    /// <param name="service">The service to format.</param>
    /// <returns>A markdown formatted string.</returns>
    string ToMarkdown(DiscoveredService service);

    /// <summary>
    /// Formats a collection of discovered services as a Comma-Separated Values (CSV) table.
    /// </summary>
    /// <param name="services">The services to serialize.</param>
    /// <returns>A CSV formatted table string.</returns>
    string ToCsv(IEnumerable<DiscoveredService> services);
}

