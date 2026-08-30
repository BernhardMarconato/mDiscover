using System.Net;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;

namespace mDiscover.Core.Services;

/// <summary>
/// A mock/fake DNS-SD discovery provider for UI testing, debugging, and capturing consistent screenshots.
/// Provides realistic neutral devices (AirPlay, Cast, Printers, Smart Home, Routers, NAS) and edge cases (shimmering, resolution errors).
/// </summary>
public class FakeDnsSdProvider : IDnsSdDiscoveryProvider
{
    public const string ProviderIdentifier = "fake";

    public bool SupportsWildcardDiscovery => true;
    public DiscoveryState State { get; private set; } = DiscoveryState.Idle;

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>? ServiceDiscovered;
    public event EventHandler<IDnsSdDiscoveryProvider, ServiceUpdatedEventArgs>? ServiceUpdated;
    public event EventHandler<IDnsSdDiscoveryProvider, ServiceLostEventArgs>? ServiceLost;
    public event EventHandler<IDnsSdDiscoveryProvider, DiscoveryStateChangedEventArgs>? StateChanged;

    private readonly TimeProvider _timeProvider;
    private readonly List<DiscoveredService> _mockCatalog = [];
    private CancellationTokenSource? _discoveryCts;

    public FakeDnsSdProvider(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        InitializeMockCatalog();
    }

    private void InitializeMockCatalog()
    {
        _mockCatalog.Clear();
        var now = _timeProvider.GetUtcNow();

        // 1. Apple Device - iPhone
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "iPhone 15 Pro._companion-link._tcp.local.",
            InstanceName = "iPhone 15 Pro",
            ServiceType = "_companion-link._tcp",
            Domain = "local.",
            HostName = "iPhone-15-Pro.local.",
            Port = 50123,
            IPv4Addresses = [IPAddress.Parse("192.168.1.101")],
            IPv6Addresses = [IPAddress.Parse("fe80::1001")],
            ProviderId = ProviderIdentifier,
            FirstSeen = now,
            LastSeen = now,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("rpBA", "58:D3:49:AB:CD:EF"),
                new("rpFl", "0x20000"),
                new("model", "iPhone16,1")
            ]
        });

        // 2. Apple Device - iPad
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "iPad Pro._companion-link._tcp.local.",
            InstanceName = "iPad Pro",
            ServiceType = "_companion-link._tcp",
            Domain = "local.",
            HostName = "iPad-Pro.local.",
            Port = 49152,
            IPv4Addresses = [IPAddress.Parse("192.168.1.105")],
            IPv6Addresses = [IPAddress.Parse("fe80::2045")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("rpBA", "34:08:BC:11:22:33"),
                new("model", "iPad14,3")
            ]
        });

        // 3. Smart TV - Google Cast
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Living Room TV._googlecast._tcp.local.",
            InstanceName = "Living Room TV",
            ServiceType = "_googlecast._tcp",
            Domain = "local.",
            HostName = "Living-Room-TV.local.",
            Port = 8009,
            IPv4Addresses = [IPAddress.Parse("192.168.0.37")],
            IPv6Addresses = [IPAddress.Parse("fe80::a00:27ff:fe4e:66a1")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("md", "Chromecast Ultra"),
                new("fn", "Living Room TV"),
                new("ca", "4101"),
                new("st", "0"),
                new("rs", "Media Player")
            ]
        });

        // 4. Smart TV - Android TV Remote (Same Host & IP to verify multi-service device grouping)
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Living Room TV._androidtvremote2._tcp.local.",
            InstanceName = "Living Room TV",
            ServiceType = "_androidtvremote2._tcp",
            Domain = "local.",
            HostName = "Living-Room-TV.local.",
            Port = 6466,
            IPv4Addresses = [IPAddress.Parse("192.168.0.37")],
            IPv6Addresses = [IPAddress.Parse("fe80::a00:27ff:fe4e:66a1")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("bt", "1"),
                new("version", "2")
            ]
        });

        // 5. Elgato Key Light
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Elgato Key Light 4E1A._elg._tcp.local.",
            InstanceName = "Elgato Key Light 4E1A",
            ServiceType = "_elg._tcp",
            Domain = "local.",
            HostName = "elgato-key-light-4e1a.local.",
            Port = 9123,
            IPv4Addresses = [IPAddress.Parse("192.168.1.42")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("mf", "Elgato"),
                new("md", "Elgato Key Light"),
                new("id", "CW38J1A01234"),
                new("pv", "1.0.3")
            ]
        });

        // 6. Philips Hue Bridge
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Philips Hue - 0017882CA1B2._hue._tcp.local.",
            InstanceName = "Philips Hue - 0017882CA1B2",
            ServiceType = "_hue._tcp",
            Domain = "local.",
            HostName = "Philips-hue.local.",
            Port = 443,
            IPv4Addresses = [IPAddress.Parse("192.168.1.50")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("bridgeid", "0017882CA1B2"),
                new("modelid", "BSB002")
            ]
        });

        // 7. Router / Gateway (TR-369 USP)
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Gateway Router._usp-agt-mqtt._tcp.local.",
            InstanceName = "Gateway Router",
            ServiceType = "_usp-agt-mqtt._tcp",
            Domain = "local.",
            HostName = "gateway-router.local.",
            Port = 1883,
            IPv4Addresses = [IPAddress.Parse("192.168.0.1")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("path", "/usp/endpoint"),
                new("proto", "mqtt")
            ]
        });

        // 8. Network Laser Printer & Scanner (IPP)
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Office Color LaserJet Pro._ipp._tcp.local.",
            InstanceName = "Office Color LaserJet Pro",
            ServiceType = "_ipp._tcp",
            Domain = "local.",
            HostName = "office-printer-m479.local.",
            Port = 631,
            IPv4Addresses = [IPAddress.Parse("192.168.1.80")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("txtvers", "1"),
                new("qtotal", "1"),
                new("rp", "ipp/print"),
                new("ty", "Office Color LaserJet Pro"),
                new("Color", "T"),
                new("Duplex", "T"),
                new("Scan", "T")
            ]
        });

        // 9. NAS Storage (SMB & SSH File Server)
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Network Storage Server._smb._tcp.local.",
            InstanceName = "Network Storage Server",
            ServiceType = "_smb._tcp",
            Domain = "local.",
            HostName = "nas-storage.local.",
            Port = 445,
            IPv4Addresses = [IPAddress.Parse("192.168.1.200")],
            IPv6Addresses = [IPAddress.Parse("2001:db8::200")],
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolved,
            TxtRecords =
            [
                new("model", "DS920+"),
                new("dsm", "7.2.1-69057")
            ]
        });

        // 10. Shimmer Placeholder Demo (Resolving in progress)
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Smart Sensor Node._matter._tcp.local.",
            InstanceName = "Smart Sensor Node 82A1",
            ServiceType = "_matter._tcp",
            Domain = "local.",
            HostName = "sensor-node-82a1.local.",
            Port = 5540,
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Resolving
        });

        // 11. Error State Demo (Failed Resolution with retry capability)
        _mockCatalog.Add(new DiscoveredService
        {
            Id = "Legacy IoT Hub._http._tcp.local.",
            InstanceName = "Legacy IoT Hub",
            ServiceType = "_http._tcp",
            Domain = "local.",
            HostName = "legacy-hub.local.",
            Port = 80,
            ProviderId = ProviderIdentifier,
            ResolutionStatus = ResolutionStatus.Failed,
            FailureReason = ResolutionFailureReason.NoAddressesFound,
            FailureDetails = "DNS A/AAAA query returned 0 records for legacy-hub.local"
        });
    }

    public async Task StartDiscoveryAsync(DiscoveryOptions options, CancellationToken ct = default)
    {
        _discoveryCts?.Cancel();
        _discoveryCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _discoveryCts.Token;

        State = DiscoveryState.Discovering;
        StateChanged?.Invoke(this, new DiscoveryStateChangedEventArgs(DiscoveryState.Discovering, "Fake discovery active"));

        InitializeMockCatalog();

        try
        {
            // Simulate realistic arrival of network services with slight staggered delays
            foreach (var service in _mockCatalog)
            {
                if (token.IsCancellationRequested)
                    break;

                await Task.Delay(40, token);
                ServiceDiscovered?.Invoke(this, new ServiceDiscoveredEventArgs(service));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    public Task StopDiscoveryAsync()
    {
        _discoveryCts?.Cancel();
        State = DiscoveryState.Idle;
        StateChanged?.Invoke(this, new DiscoveryStateChangedEventArgs(DiscoveryState.Idle));
        return Task.CompletedTask;
    }

    public async Task<DiscoveredService?> ResolveDetailsAsync(DiscoveredService service, CancellationToken ct = default)
    {
        await Task.Delay(250, ct);

        // If the service was previously failed or resolving, simulate successful resolution
        service.ResolutionStatus = ResolutionStatus.Resolved;
        service.FailureReason = ResolutionFailureReason.None;
        service.FailureDetails = null;

        if (service.IPv4Addresses.Count == 0)
        {
            service.IPv4Addresses.Add(IPAddress.Parse("192.168.1.199"));
        }

        if (string.IsNullOrWhiteSpace(service.HostName))
        {
            service.HostName = $"{service.InstanceName.ToLowerInvariant().Replace(' ', '-')}.local.";
        }

        service.LastSeen = _timeProvider.GetUtcNow();
        ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(service));
        return service;
    }

    public void SimulateServiceLost(string serviceId)
    {
        ServiceLost?.Invoke(this, new ServiceLostEventArgs(serviceId));
    }

    public void SimulateServiceDiscovered(DiscoveredService service)
    {
        ServiceDiscovered?.Invoke(this, new ServiceDiscoveredEventArgs(service));
    }

    public void SimulateServiceUpdated(DiscoveredService service)
    {
        ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(service));
    }

    public ValueTask DisposeAsync()
    {
        _discoveryCts?.Cancel();
        _discoveryCts?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
