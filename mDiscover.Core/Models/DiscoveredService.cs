using System.Net;

namespace mDiscover.Core.Models;

/// <summary>
/// Domain entity representing a discovered DNS-SD/Bonjour service on the local network.
/// </summary>
public class DiscoveredService
{
    public required string Id { get; init; }
    public required string InstanceName { get; set; }
    public required string ServiceType { get; set; }
    public required string Domain { get; set; }
    public string? HostName { get; set; }
    public int? Port { get; set; }
    public List<TxtRecordItem> TxtRecords { get; set; } = [];
    public List<IPAddress> IPv4Addresses { get; set; } = [];
    public List<IPAddress> IPv6Addresses { get; set; } = [];
    public required string ProviderId { get; init; }
    public DateTimeOffset FirstSeen { get; init; }
    public DateTimeOffset LastSeen { get; set; }
    public bool IsOnline { get; set; } = true;
    public ResolutionStatus ResolutionStatus { get; set; } = ResolutionStatus.Resolving;
    public ResolutionFailureReason FailureReason { get; set; } = ResolutionFailureReason.None;
    public string? FailureDetails { get; set; }
    public bool IsFallbackResolution { get; set; }

    public ServiceDefinition ServiceDefinition => WellKnownServiceCatalog.GetOrInfer(ServiceType);
    public ServiceCategory Category => ServiceDefinition.Category;
    public string DisplayType => ServiceDefinition.DisplayName;
    public string FullServicePath => $"{InstanceName}.{ServiceType}.{Domain}";

    public IPAddress? PrimaryIpAddress => IPv4Addresses.Count > 0 ? IPv4Addresses[0] : (IPv6Addresses.Count > 0 ? IPv6Addresses[0] : null);
    public string PrimaryIp => PrimaryIpAddress?.ToString() ?? string.Empty;
    public string FormattedEndpoint => !string.IsNullOrWhiteSpace(PrimaryIp) ? (Port.HasValue ? $"{PrimaryIp}:{Port}" : PrimaryIp) : (!string.IsNullOrWhiteSpace(HostName) ? (Port.HasValue ? $"{HostName}:{Port}" : HostName) : string.Empty);

    /// <summary>
    /// Gets a value indicating whether this service exposes a web UI reachable in a web browser.
    /// </summary>
    public bool CanOpenInBrowser =>
        (Port is 80 or 443 or 8080 or 8000 or 8443 or 8123 or 9123 or 5000 or 7125 or 32400 ||
         ServiceType.Contains("http", StringComparison.OrdinalIgnoreCase) ||
         ServiceType.Contains("web", StringComparison.OrdinalIgnoreCase) ||
         ServiceDefinition.Transport is "http" or "https") &&
        (!string.IsNullOrWhiteSpace(PrimaryIp) || !string.IsNullOrWhiteSpace(HostName));

    /// <summary>
    /// Gets the formatted web browser URL for the service endpoint, or null if not a web service.
    /// </summary>
    public string? BrowserUrl
    {
        get
        {
            if (!CanOpenInBrowser)
                return null;

            var scheme = (Port is 443 or 8443 || ServiceDefinition.Transport == "https") ? "https" : "http";
            var targetHost = !string.IsNullOrWhiteSpace(PrimaryIp) ? PrimaryIp : HostName;
            return (Port is 80 or 443) ? $"{scheme}://{targetHost}" : $"{scheme}://{targetHost}:{Port}";
        }
    }
}

