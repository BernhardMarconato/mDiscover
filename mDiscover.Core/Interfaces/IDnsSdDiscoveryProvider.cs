using mDiscover.Core.Models;

namespace mDiscover.Core.Interfaces;

/// <summary>
/// Defines the contract for DNS-SD / Bonjour discovery provider implementations (e.g. Win32 native, WinRT DeviceWatcher).
/// </summary>
public interface IDnsSdDiscoveryProvider : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this provider supports wildcard discovery via DNS-SD meta-queries ("_services._dns-sd._udp").
    /// </summary>
    bool SupportsWildcardDiscovery { get; }

    /// <summary>
    /// Gets the current lifecycle state of this discovery provider.
    /// </summary>
    DiscoveryState State { get; }

    /// <summary>
    /// Raised when a new service instance is discovered on the local network.
    /// </summary>
    event EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>? ServiceDiscovered;

    /// <summary>
    /// Raised when an existing service instance is updated with resolved endpoints, addresses, or TXT records.
    /// </summary>
    event EventHandler<IDnsSdDiscoveryProvider, ServiceUpdatedEventArgs>? ServiceUpdated;

    /// <summary>
    /// Raised when a service instance is no longer reachable or has unregistered from the network.
    /// </summary>
    event EventHandler<IDnsSdDiscoveryProvider, ServiceLostEventArgs>? ServiceLost;

    /// <summary>
    /// Raised when the provider's discovery state changes.
    /// </summary>
    event EventHandler<IDnsSdDiscoveryProvider, DiscoveryStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Starts asynchronous DNS-SD service discovery with the specified scan options.
    /// </summary>
    /// <param name="options">The scan configuration and target service types.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    Task StartDiscoveryAsync(DiscoveryOptions options, CancellationToken ct = default);

    /// <summary>
    /// Stops active service discovery and frees background query resources.
    /// </summary>
    Task StopDiscoveryAsync();

    /// <summary>
    /// Asynchronously performs deep resolution on a service instance to resolve IP addresses, port, and TXT attributes.
    /// </summary>
    /// <param name="service">The service to resolve.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The resolved service instance, or null if resolution failed.</returns>
    Task<DiscoveredService?> ResolveDetailsAsync(DiscoveredService service, CancellationToken ct = default);
}

