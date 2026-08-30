namespace mDiscover.Core.Interfaces;

/// <summary>
/// Coordinates multiple DNS-SD discovery providers, manages active provider selection, and delegates discovery operations.
/// </summary>
public interface IServiceDiscoveryEngine : IDnsSdDiscoveryProvider
{
    /// <summary>
    /// Gets the list of all registered discovery providers available in the application.
    /// </summary>
    IReadOnlyList<IDnsSdDiscoveryProvider> AvailableProviders { get; }

    /// <summary>
    /// Gets the currently active discovery provider.
    /// </summary>
    IDnsSdDiscoveryProvider ActiveProvider { get; }

    /// <summary>
    /// Sets the active discovery provider to the specified instance.
    /// </summary>
    /// <param name="provider">The provider to activate.</param>
    Task SetActiveProviderAsync(IDnsSdDiscoveryProvider provider);

    /// <summary>
    /// Sets the active discovery provider by its registered provider identifier (e.g. "win32", "winrt").
    /// </summary>
    /// <param name="providerId">The identifier of the provider to activate.</param>
    Task SetActiveProviderAsync(string providerId);

    /// <summary>
    /// Resolves the application identifier for the specified provider instance.
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <returns>The assigned provider identifier string.</returns>
    string GetProviderId(IDnsSdDiscoveryProvider provider);
}

