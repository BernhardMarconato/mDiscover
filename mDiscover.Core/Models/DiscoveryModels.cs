namespace mDiscover.Core.Models;

/// <summary>
/// Defines discovery scan strategies used by DNS-SD discovery providers.
/// </summary>
public enum DiscoveryMode
{
    /// <summary>
    /// Issues a single meta-query ("_services._dns-sd._udp") to discover all active service types on the network dynamically.
    /// </summary>
    WildcardMeta,

    /// <summary>
    /// Probes a curated/configured list of specific well-known DNS-SD service types.
    /// </summary>
    TargetedList,

    /// <summary>
    /// Combines wildcard meta-queries with concurrent targeted list probing for maximum network coverage.
    /// </summary>
    Hybrid
}

/// <summary>
/// Defines the runtime lifecycle state of the DNS-SD discovery engine or provider.
/// </summary>
public enum DiscoveryState
{
    /// <summary>
    /// Discovery is stopped and the provider is idle.
    /// </summary>
    Idle,

    /// <summary>
    /// Discovery queries and network watchers are actively running.
    /// </summary>
    Discovering,

    /// <summary>
    /// An unrecoverable network or provider initialization error occurred.
    /// </summary>
    Error
}

/// <summary>
/// Options configuring a service discovery session.
/// </summary>
public class DiscoveryOptions
{
    /// <summary>
    /// Gets the discovery strategy mode (WildcardMeta, TargetedList, or Hybrid).
    /// </summary>
    public DiscoveryMode Mode { get; init; } = DiscoveryMode.WildcardMeta;

    /// <summary>
    /// Gets the optional targeted service type list to probe when in TargetedList or Hybrid mode.
    /// </summary>
    public IReadOnlyList<string>? TargetServiceTypes { get; init; }

    /// <summary>
    /// Gets the mDNS/DNS domain suffix to query (defaults to "local").
    /// </summary>
    public string Domain { get; init; } = "local";
}

/// <summary>
/// Encapsulates snapshot counts of active services, distinct service types, and distinct hosts.
/// </summary>
/// <param name="ServicesCount">The total count of active discovered services.</param>
/// <param name="TypesCount">The count of distinct service types represented.</param>
/// <param name="HostsCount">The count of distinct host machines represented.</param>
public record DiscoveryStats(int ServicesCount = 0, int TypesCount = 0, int HostsCount = 0);

