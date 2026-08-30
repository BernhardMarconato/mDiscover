using System.Text;
using System.Text.Json;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Serialization;

namespace mDiscover.Core.Services;

/// <summary>
/// Implements DNS-SD service serialization to JSON, Markdown, CSV, and Plain Text with localization and testable time support.
/// </summary>
public class ExportService(ExportLocalizationOptions? options = null, TimeProvider? timeProvider = null) : IExportService
{
    private readonly ExportLocalizationOptions _options = options ?? new ExportLocalizationOptions();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public static ExportService Default { get; } = new();

    public string Export(IEnumerable<DiscoveredService> services, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => ToJson(services),
            ExportFormat.Csv => ToCsv(services),
            ExportFormat.Text => ToPlainText(services),
            ExportFormat.Markdown or _ => ToMarkdown(services)
        };
    }

    public string Export(DiscoveredService service, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => ToJson(service),
            ExportFormat.Csv => ToCsv(new[] { service }),
            ExportFormat.Text => ToPlainText(service),
            ExportFormat.Markdown or _ => ToMarkdown(service)
        };
    }

    public string ToJson(IEnumerable<DiscoveredService> services)
    {
        var list = services.ToList();
        return JsonSerializer.Serialize(list, AppJsonSerializerContext.Default.ListDiscoveredService);
    }

    public string ToJson(DiscoveredService service)
    {
        return JsonSerializer.Serialize(service, AppJsonSerializerContext.Default.DiscoveredService);
    }

    public string ToPlainText(IEnumerable<DiscoveredService> services)
    {
        var sb = new StringBuilder();
        foreach (var s in services)
        {
            sb.AppendLine(ToPlainText(s));
            sb.AppendLine("----------------------------------------");
        }
        return sb.ToString();
    }

    public string ToPlainText(DiscoveredService service)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{_options.ServiceLabel}: {service.InstanceName}");
        sb.AppendLine($"{_options.TypeLabel}: {service.ServiceType}");
        sb.AppendLine($"{_options.HostLabel}: {service.HostName ?? _options.UnspecifiedLabel}");
        sb.AppendLine($"{_options.EndpointLabel}: {service.FormattedEndpoint}");
        if (service.IPv4Addresses.Count > 0)
            sb.AppendLine($"{_options.IPv4Label}: {string.Join(", ", service.IPv4Addresses)}");
        if (service.IPv6Addresses.Count > 0)
            sb.AppendLine($"{_options.IPv6Label}: {string.Join(", ", service.IPv6Addresses)}");
        if (service.TxtRecords.Count > 0)
        {
            sb.AppendLine($"{_options.TxtAttributesLabel}:");
            foreach (var txt in service.TxtRecords)
            {
                sb.AppendLine($"  {txt.Key} = {txt.Value}");
            }
        }
        return sb.ToString();
    }

    public string ToMarkdown(DiscoveredService service)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### {service.InstanceName}");
        sb.AppendLine();
        sb.AppendLine($"- **{_options.TypeLabel}:** `{service.ServiceType}`");
        sb.AppendLine($"- **{_options.HostLabel}:** `{service.HostName ?? _options.UnspecifiedLabel}`");
        sb.AppendLine($"- **{_options.EndpointLabel}:** `{service.FormattedEndpoint}`");
        if (service.IPv4Addresses.Count > 0)
            sb.AppendLine($"- **{_options.IPv4Label}:** {string.Join(", ", service.IPv4Addresses)}");
        if (service.IPv6Addresses.Count > 0)
            sb.AppendLine($"- **{_options.IPv6Label}:** {string.Join(", ", service.IPv6Addresses)}");
        if (service.TxtRecords.Count > 0)
        {
            sb.AppendLine($"- **{_options.TxtAttributesLabel}:**");
            foreach (var txt in service.TxtRecords)
            {
                sb.AppendLine($"  - `{txt.Key}`: {txt.Value}");
            }
        }
        return sb.ToString();
    }

    public string ToCsv(IEnumerable<DiscoveredService> services)
    {
        var sb = new StringBuilder();
        sb.AppendLine("InstanceName,ServiceType,Domain,HostName,Port,IPv4,IPv6,TxtRecords,FirstSeen,LastSeen,IsOnline");

        foreach (var s in services)
        {
            var txtStr = string.Join(";", s.TxtRecords.Select(t => $"{t.Key}={t.Value}"));
            var ip4Str = string.Join(";", s.IPv4Addresses);
            var ip6Str = string.Join(";", s.IPv6Addresses);

            sb.AppendLine(string.Join(",",
                EscapeCsv(s.InstanceName),
                EscapeCsv(s.ServiceType),
                EscapeCsv(s.Domain),
                EscapeCsv(s.HostName ?? string.Empty),
                s.Port?.ToString() ?? string.Empty,
                EscapeCsv(ip4Str),
                EscapeCsv(ip6Str),
                EscapeCsv(txtStr),
                s.FirstSeen.ToString("O"),
                s.LastSeen.ToString("O"),
                s.IsOnline.ToString()
            ));
        }

        return sb.ToString();
    }

    public string ToMarkdown(IEnumerable<DiscoveredService> services)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {_options.MarkdownDocumentTitle}");
        sb.AppendLine();
        var now = _timeProvider.GetLocalNow();
        sb.AppendLine($"*{_options.ExportedOnPrefix} {now:yyyy-MM-dd HH:mm:ss}*");
        sb.AppendLine();
        sb.AppendLine($"| {_options.TableHeaderServiceName} | {_options.TableHeaderType} | {_options.TableHeaderHost} | {_options.TableHeaderEndpoint} | {_options.TableHeaderIpAddresses} | {_options.TableHeaderTxtAttributes} |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");

        foreach (var s in services)
        {
            var txt = s.TxtRecords.Count > 0 ? string.Join("<br>", s.TxtRecords.Select(t => $"`{t.Key}={t.Value}`")) : "—";
            var ips = s.IPv4Addresses.Count > 0 ? string.Join(", ", s.IPv4Addresses) : (s.IPv6Addresses.Count > 0 ? string.Join(", ", s.IPv6Addresses) : "—");
            var host = s.HostName ?? "—";
            var ep = s.FormattedEndpoint;
            if (string.IsNullOrWhiteSpace(ep))
                ep = "—";

            sb.AppendLine($"| **{EscapeMd(s.InstanceName)}** | `{s.ServiceType}` | {EscapeMd(host)} | `{ep}` | {ips} | {txt} |");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static string EscapeMd(string value)
    {
        return value.Replace("|", "\\|").Replace("\n", " ");
    }
}
