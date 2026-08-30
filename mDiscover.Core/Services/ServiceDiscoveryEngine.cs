using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;

namespace mDiscover.Core.Services;

/// <summary>
/// Default implementation of <see cref="IServiceDiscoveryEngine"/> coordinating registered DNS-SD discovery providers.
/// </summary>
public class ServiceDiscoveryEngine : IServiceDiscoveryEngine
{
    private readonly IEnumerable<IDnsSdDiscoveryProvider> _providers;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ServiceDiscoveryEngine> _logger;

    public bool SupportsWildcardDiscovery => ActiveProvider.SupportsWildcardDiscovery;

    public DiscoveryState State => ActiveProvider.State;

    public IReadOnlyList<IDnsSdDiscoveryProvider> AvailableProviders => _providers.ToList();

    public IDnsSdDiscoveryProvider ActiveProvider { get; private set; }

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceDiscoveredEventArgs>? ServiceDiscovered;

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceUpdatedEventArgs>? ServiceUpdated;

    public event EventHandler<IDnsSdDiscoveryProvider, ServiceLostEventArgs>? ServiceLost;

    public event EventHandler<IDnsSdDiscoveryProvider, DiscoveryStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceDiscoveryEngine"/> class.
    /// </summary>
    /// <param name="providers">The collection of available DNS-SD discovery providers.</param>
    /// <param name="settingsService">The application settings service for reading/persisting preferred provider.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ServiceDiscoveryEngine(
        IEnumerable<IDnsSdDiscoveryProvider> providers,
        ISettingsService settingsService,
        ILogger<ServiceDiscoveryEngine>? logger = null)
    {
        _providers = providers;
        _settingsService = settingsService;
        _logger = logger ?? NullLogger<ServiceDiscoveryEngine>.Instance;

        var preferred = _settingsService.ReadSetting(SettingDefinitions.PreferredProvider);
        ActiveProvider = _providers.FirstOrDefault(p => GetProviderId(p).Equals(preferred, StringComparison.OrdinalIgnoreCase))
                          ?? _providers.FirstOrDefault()
                          ?? throw new InvalidOperationException("No DNS-SD discovery provider available.");

        HookProviderEvents(ActiveProvider);
        _logger.LogInformation("Initialized ServiceDiscoveryEngine with active provider '{Id}'", GetProviderId(ActiveProvider));
    }

    public string GetProviderId(IDnsSdDiscoveryProvider provider)
    {
        var typeName = provider.GetType().Name;
        if (typeName.StartsWith("Win32", StringComparison.OrdinalIgnoreCase))
            return "win32";
        if (typeName.StartsWith("WinRt", StringComparison.OrdinalIgnoreCase))
            return "winrt";
        if (typeName.StartsWith("Fake", StringComparison.OrdinalIgnoreCase))
            return "fake";
        return typeName.ToLowerInvariant();
    }

    public Task SetActiveProviderAsync(IDnsSdDiscoveryProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return SetActiveProviderCoreAsync(provider);
    }

    public Task SetActiveProviderAsync(string providerId)
    {
        var target = _providers.FirstOrDefault(p => GetProviderId(p).Equals(providerId, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            throw new ArgumentException($"Provider '{providerId}' is not registered.", nameof(providerId));
        }

        return SetActiveProviderCoreAsync(target);
    }

    private async Task SetActiveProviderCoreAsync(IDnsSdDiscoveryProvider target)
    {
        if (ActiveProvider == target)
            return;

        var oldId = GetProviderId(ActiveProvider);
        var newId = GetProviderId(target);
        _logger.LogInformation("Switching active discovery provider from '{Old}' to '{New}'", oldId, newId);

        if (ActiveProvider.State == DiscoveryState.Discovering)
        {
            await ActiveProvider.StopDiscoveryAsync();
        }

        UnhookProviderEvents(ActiveProvider);
        ActiveProvider = target;
        HookProviderEvents(ActiveProvider);

        _settingsService.SaveSetting(SettingDefinitions.PreferredProvider, newId);

        StateChanged?.Invoke(this, new DiscoveryStateChangedEventArgs(ActiveProvider.State, $"Switched engine to {newId}"));
    }

    public Task StartDiscoveryAsync(DiscoveryOptions options, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting discovery on engine with provider '{ProviderId}'", GetProviderId(ActiveProvider));
        return ActiveProvider.StartDiscoveryAsync(options, ct);
    }

    public Task StopDiscoveryAsync()
    {
        _logger.LogInformation("Stopping discovery on engine");
        return ActiveProvider.StopDiscoveryAsync();
    }

    public Task<DiscoveredService?> ResolveDetailsAsync(DiscoveredService service, CancellationToken ct = default)
    {
        return ActiveProvider.ResolveDetailsAsync(service, ct);
    }

    private void HookProviderEvents(IDnsSdDiscoveryProvider provider)
    {
        provider.ServiceDiscovered += OnProviderServiceDiscovered;
        provider.ServiceUpdated += OnProviderServiceUpdated;
        provider.ServiceLost += OnProviderServiceLost;
        provider.StateChanged += OnProviderStateChanged;
    }

    private void UnhookProviderEvents(IDnsSdDiscoveryProvider provider)
    {
        provider.ServiceDiscovered -= OnProviderServiceDiscovered;
        provider.ServiceUpdated -= OnProviderServiceUpdated;
        provider.ServiceLost -= OnProviderServiceLost;
        provider.StateChanged -= OnProviderStateChanged;
    }

    private void OnProviderServiceDiscovered(IDnsSdDiscoveryProvider sender, ServiceDiscoveredEventArgs e) => ServiceDiscovered?.Invoke(this, e);
    private void OnProviderServiceUpdated(IDnsSdDiscoveryProvider sender, ServiceUpdatedEventArgs e) => ServiceUpdated?.Invoke(this, e);
    private void OnProviderServiceLost(IDnsSdDiscoveryProvider sender, ServiceLostEventArgs e) => ServiceLost?.Invoke(this, e);
    private void OnProviderStateChanged(IDnsSdDiscoveryProvider sender, DiscoveryStateChangedEventArgs e) => StateChanged?.Invoke(this, e);

    public async ValueTask DisposeAsync()
    {
        foreach (var p in _providers)
        {
            await p.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
