using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Devices.Enumeration;
using mDiscover.Core.Common;
using mDiscover.Core.Extensions;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Services;

namespace mDiscover.Providers.WinRt;

/// <summary>
/// DNS-SD discovery provider leveraging the Windows Runtime (WinRT) <see cref="DeviceWatcher"/> API.
/// Queries Association Endpoint Services (<c>AssociationEndpointService</c>) matching the DNS-SD protocol GUID (<c>{4526e8c1-8aac-4153-9b16-55e86ada0e54}</c>)
/// using Advanced Query Syntax (AQS).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WinRtDeviceWatcherProvider"/> class.
/// </remarks>
/// <param name="timeProvider">Optional time provider for timestamps.</param>
/// <param name="logger">Optional logger instance.</param>
public class WinRtDeviceWatcherProvider(TimeProvider? timeProvider = null, ILogger<WinRtDeviceWatcherProvider>? logger = null) : IDnsSdDiscoveryProvider
{
    public bool SupportsWildcardDiscovery => false;

    private const string DnsSdProtocolId = "{4526e8c1-8aac-4153-9b16-55e86ada0e54}";

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly ILogger<WinRtDeviceWatcherProvider> _logger = logger ?? NullLogger<WinRtDeviceWatcherProvider>.Instance;

    public DiscoveryState State
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                StateChanged?.Invoke(this, new DiscoveryStateChangedEventArgs(value));
            }
        }
    } = DiscoveryState.Idle;

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>? ServiceDiscovered;

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceUpdatedEventArgs>? ServiceUpdated;

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceLostEventArgs>? ServiceLost;

    public event EventHandler<IDnsSdDiscoveryProvider, DiscoveryStateChangedEventArgs>? StateChanged;

    private DeviceWatcher? _watcher;
    private readonly ConcurrentDictionary<string, DiscoveredService> _discoveredServices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    private static readonly string[] _requestedProperties =
    [
        "System.Devices.Dnssd.InstanceName",
        "System.Devices.Dnssd.ServiceName",
        "System.Devices.Dnssd.Domain",
        "System.Devices.Dnssd.HostName",
        "System.Devices.Dnssd.PortNumber",
        "System.Devices.Dnssd.TextAttributes",
        "System.Devices.IpAddress",
        "System.ItemNameDisplay"
    ];

    public Task StartDiscoveryAsync(DiscoveryOptions options, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            StopInternal();

            lock (_lock)
            {
                _discoveredServices.Clear();
                State = DiscoveryState.Discovering;

                var scanTypes = options.TargetServiceTypes != null && options.TargetServiceTypes.Count > 0
                    ? options.TargetServiceTypes
                    : WellKnownServiceCatalog.CommonScanTypes;

                var allTypes = new HashSet<string>(scanTypes, StringComparer.OrdinalIgnoreCase)
                {
                    "_services._dns-sd._udp"
                };

                var typeFilters = allTypes.Select(st => $"System.Devices.Dnssd.ServiceName:=\"{st}\"");
                var joinedTypes = string.Join(" OR ", typeFilters);
                var aqsFilter = $"System.Devices.AepService.ProtocolId:=\"{DnsSdProtocolId}\" AND ({joinedTypes})";

                _logger.LogInformation("Starting single merged DNS-SD DeviceWatcher with {Count} service types", allTypes.Count);

                try
                {
                    _watcher = DeviceInformation.CreateWatcher(
                        aqsFilter,
                        _requestedProperties,
                        DeviceInformationKind.AssociationEndpointService
                    );

                    _watcher.Added += OnDeviceAdded;
                    _watcher.Updated += OnDeviceUpdated;
                    _watcher.Removed += OnDeviceRemoved;
                    _watcher.EnumerationCompleted += OnEnumerationCompleted;
                    _watcher.Stopped += OnWatcherStopped;

                    _watcher.Start();
                    _logger.LogInformation("DNS-SD DeviceWatcher successfully started");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start DNS-SD DeviceWatcher");
                    State = DiscoveryState.Error;
                    throw;
                }
            }
        }, ct);
    }

    private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation info)
    {
        try
        {
            var service = ParseDeviceInformation(info);
            if (service == null)
                return;

            _discoveredServices[service.Id] = service;
            _logger.LogInformation("Discovered service [{ServiceType}]: '{InstanceName}' on host '{Host}' ({Endpoint})",
                service.ServiceType, service.InstanceName, service.HostName, service.FormattedEndpoint);

            ServiceDiscovered?.Invoke(this, new ServiceDiscoveredEventArgs(service));
            _ = Task.Run(() => ResolveIpAddressesAsync(service));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing added device ({DeviceId})", info.Id);
        }
    }

    private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        try
        {
            if (_discoveredServices.TryGetValue(update.Id, out var existing))
            {
                UpdateServiceProperties(existing, update);
                existing.LastSeen = _timeProvider.GetUtcNow();
                existing.IsOnline = true;
                _logger.LogDebug("Updated service [{ServiceType}]: '{InstanceName}'", existing.ServiceType, existing.InstanceName);
                ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(existing));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing updated device ({DeviceId})", update.Id);
        }
    }

    private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        try
        {
            if (_discoveredServices.TryRemove(update.Id, out var removed))
            {
                removed.IsOnline = false;
                _logger.LogInformation("Lost service [{ServiceType}]: '{InstanceName}'", removed.ServiceType, removed.InstanceName);
                ServiceLost?.Invoke(this, new ServiceLostEventArgs(removed.Id));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing removed device ({DeviceId})", update.Id);
        }
    }

    private void OnEnumerationCompleted(DeviceWatcher sender, object args)
    {
        _logger.LogInformation("Initial DNS-SD network enumeration completed. Total services cached: {Count}", _discoveredServices.Count);
    }

    private void OnWatcherStopped(DeviceWatcher sender, object args)
    {
        _logger.LogInformation("DNS-SD DeviceWatcher stopped");
    }

    private DiscoveredService? ParseDeviceInformation(DeviceInformation info)
    {
        if (string.IsNullOrWhiteSpace(info.Id))
            return null;

        var instanceName = info.Properties.TryGetProperty<string>("System.Devices.Dnssd.InstanceName")
                        ?? info.Name
                        ?? info.Properties.TryGetProperty<string>("System.ItemNameDisplay")
                        ?? "Unnamed Instance";

        var serviceType = info.Properties.TryGetProperty<string>("System.Devices.Dnssd.ServiceName")
                       ?? "_unknown._tcp";

        var domain = info.Properties.TryGetProperty<string>("System.Devices.Dnssd.Domain")
                  ?? "local";

        var hostName = info.Properties.TryGetProperty<string>("System.Devices.Dnssd.HostName");
        var port = info.Properties.TryGetProperty<int?>("System.Devices.Dnssd.PortNumber");
        var txtRecords = ParseTxtAttributes(info.Properties);

        var service = new DiscoveredService
        {
            Id = info.Id,
            InstanceName = instanceName,
            ServiceType = serviceType,
            Domain = domain,
            HostName = hostName,
            Port = port,
            TxtRecords = txtRecords,
            ProviderId = "winrt",
            IsOnline = true,
            FirstSeen = _timeProvider.GetUtcNow(),
            LastSeen = _timeProvider.GetUtcNow(),
            ResolutionStatus = ResolutionStatus.Resolving
        };

        var ipObj = info.Properties.TryGetProperty<object>("System.Devices.IpAddress");
        if (ipObj != null)
        {
            AddIpAddresses(service, ipObj);
        }

        if (service.IPv4Addresses.Count > 0 || service.IPv6Addresses.Count > 0)
        {
            service.ResolutionStatus = ResolutionStatus.Resolved;
        }
        else if (string.IsNullOrWhiteSpace(service.HostName))
        {
            service.ResolutionStatus = ResolutionStatus.Failed;
            service.FailureReason = ResolutionFailureReason.NoHostName;
        }

        return service;
    }

    private void UpdateServiceProperties(DiscoveredService service, DeviceInformationUpdate update)
    {
        if (update.Properties == null)
            return;

        var host = update.Properties.TryGetProperty<string>("System.Devices.Dnssd.HostName");
        if (!string.IsNullOrWhiteSpace(host))
        {
            service.HostName = host;
        }

        var port = update.Properties.TryGetProperty<int?>("System.Devices.Dnssd.PortNumber");
        if (port.HasValue && port.Value > 0)
        {
            service.Port = port.Value;
        }

        var instName = update.Properties.TryGetProperty<string>("System.Devices.Dnssd.InstanceName");
        if (!string.IsNullOrWhiteSpace(instName))
        {
            service.InstanceName = instName;
        }

        var updatedTxt = ParseTxtAttributes(update.Properties);
        if (updatedTxt.Count > 0)
        {
            service.TxtRecords = updatedTxt;
        }

        var ipObj = update.Properties.TryGetProperty<object>("System.Devices.IpAddress");
        if (ipObj != null)
        {
            AddIpAddresses(service, ipObj);
        }
    }

    private static void AddIpAddresses(DiscoveredService service, object ipObj)
    {
        if (ipObj is string[] ipArray)
        {
            foreach (var ip in ipArray)
                AddIpAddress(service, ip);
        }
        else if (ipObj is string ipStr)
        {
            AddIpAddress(service, ipStr);
        }
        else if (ipObj is IEnumerable<string> ipList)
        {
            foreach (var ip in ipList)
                AddIpAddress(service, ip);
        }
    }

    private static List<TxtRecordItem> ParseTxtAttributes(IReadOnlyDictionary<string, object> properties)
    {
        return TxtRecordParser.ParseFromObject(properties.TryGetProperty<object>("System.Devices.Dnssd.TextAttributes"));
    }

    private static void AddIpAddress(DiscoveredService service, string rawIp)
    {
        if (IPAddress.TryParse(rawIp, out var parsed) && parsed.IsValidHostAddress())
        {
            if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                if (!service.IPv4Addresses.Contains(parsed))
                    service.IPv4Addresses.Add(parsed);
            }
            else if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (!service.IPv6Addresses.Contains(parsed))
                    service.IPv6Addresses.Add(parsed);
            }
        }
    }

    private async Task ResolveIpAddressesAsync(DiscoveredService service)
    {
        if (string.IsNullOrWhiteSpace(service.HostName))
        {
            if (service.IPv4Addresses.Count == 0 && service.IPv6Addresses.Count == 0)
            {
                service.ResolutionStatus = ResolutionStatus.Failed;
                service.FailureReason = ResolutionFailureReason.NoHostName;
                service.FailureDetails = null;
                ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(service));
            }
            return;
        }

        var result = await DnsHostResolver.ResolveHostAddressesAsync(service.HostName).ConfigureAwait(false);
        foreach (var ip in result.IPv4Addresses)
        {
            if (!service.IPv4Addresses.Contains(ip))
                service.IPv4Addresses.Add(ip);
        }
        foreach (var ip in result.IPv6Addresses)
        {
            if (!service.IPv6Addresses.Contains(ip))
                service.IPv6Addresses.Add(ip);
        }

        if (service.IPv4Addresses.Count > 0 || service.IPv6Addresses.Count > 0)
        {
            service.ResolutionStatus = ResolutionStatus.Resolved;
            service.FailureReason = ResolutionFailureReason.None;
            service.FailureDetails = null;
        }
        else
        {
            service.ResolutionStatus = ResolutionStatus.Failed;
            service.FailureReason = result.FailureReason != ResolutionFailureReason.None ? result.FailureReason : ResolutionFailureReason.NoAddressesFound;
            service.FailureDetails = result.FailureDetails;
        }

        ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(service));
    }


    public async Task<DiscoveredService?> ResolveDetailsAsync(DiscoveredService service, CancellationToken ct = default)
    {
        service.ResolutionStatus = ResolutionStatus.Resolving;
        service.FailureReason = ResolutionFailureReason.None;
        service.FailureDetails = null;
        ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(service));

        await ResolveIpAddressesAsync(service);
        return service;
    }

    private void StopInternal()
    {
        lock (_lock)
        {
            if (_watcher != null)
            {
                try
                {
                    _watcher.Added -= OnDeviceAdded;
                    _watcher.Updated -= OnDeviceUpdated;
                    _watcher.Removed -= OnDeviceRemoved;
                    _watcher.EnumerationCompleted -= OnEnumerationCompleted;
                    _watcher.Stopped -= OnWatcherStopped;

                    if (_watcher.Status == DeviceWatcherStatus.Started ||
                        _watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                    {
                        _watcher.Stop();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Exception while stopping DeviceWatcher");
                }
                finally
                {
                    _watcher = null;
                }
            }

            State = DiscoveryState.Idle;
        }
    }

    public Task StopDiscoveryAsync()
    {
        return Task.Run(() =>
        {
            StopInternal();
            _logger.LogInformation("WinRT DNS-SD discovery stopped");
        });
    }

    public async ValueTask DisposeAsync()
    {
        await StopDiscoveryAsync();
        GC.SuppressFinalize(this);
    }
}
