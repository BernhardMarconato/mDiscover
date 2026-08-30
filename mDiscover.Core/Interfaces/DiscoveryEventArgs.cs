using mDiscover.Core.Models;

namespace mDiscover.Core.Interfaces;

/// <summary>
/// Event arguments supplied when a new DNS-SD service instance is discovered on the local network.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceDiscoveredEventArgs"/> class.
/// </remarks>
/// <param name="service">The discovered service instance.</param>
public class ServiceDiscoveredEventArgs(DiscoveredService service) : EventArgs
{
    /// <summary>
    /// Gets the newly discovered DNS-SD service instance.
    /// </summary>
    public DiscoveredService Service { get; } = service;
}

/// <summary>
/// Event arguments supplied when an existing DNS-SD service instance's metadata, endpoints, or TXT records change.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceUpdatedEventArgs"/> class.
/// </remarks>
/// <param name="service">The updated service instance.</param>
public class ServiceUpdatedEventArgs(DiscoveredService service) : EventArgs
{
    /// <summary>
    /// Gets the updated DNS-SD service instance.
    /// </summary>
    public DiscoveredService Service { get; } = service;
}

/// <summary>
/// Event arguments supplied when a DNS-SD service is removed, lost, or timed out from the network.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceLostEventArgs"/> class.
/// </remarks>
/// <param name="serviceId">The identifier of the lost service.</param>
public class ServiceLostEventArgs(string serviceId) : EventArgs
{
    /// <summary>
    /// Gets the unique identifier of the lost service.
    /// </summary>
    public string ServiceId { get; } = serviceId;
}

/// <summary>
/// Event arguments supplied when the discovery engine or provider state changes (e.g. idle, discovering, error).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DiscoveryStateChangedEventArgs"/> class.
/// </remarks>
/// <param name="newState">The new discovery state.</param>
/// <param name="statusMessage">Optional status message or error details.</param>
public class DiscoveryStateChangedEventArgs(DiscoveryState newState, string? statusMessage = null) : EventArgs
{
    /// <summary>
    /// Gets the new discovery lifecycle state.
    /// </summary>
    public DiscoveryState NewState { get; } = newState;

    /// <summary>
    /// Gets an optional human-readable status message or error details.
    /// </summary>
    public string? StatusMessage { get; } = statusMessage;
}

