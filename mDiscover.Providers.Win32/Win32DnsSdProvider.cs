using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using mDiscover.Core.Extensions;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.Dns;

namespace mDiscover.Providers.Win32;

/// <summary>
/// Native Windows DNS-SD discovery provider using the Win32 DNS API (<c>DnsServiceBrowse</c>, <c>DnsServiceResolve</c>).
/// Supports high-performance asynchronous wildcard meta-queries and native mDNS response caching.
/// </summary>
public class Win32DnsSdProvider : IDnsSdDiscoveryProvider
{
    public bool SupportsWildcardDiscovery => true;

    private static readonly TimeSpan _resolveTimeout = TimeSpan.FromSeconds(8);
    private readonly TimeProvider _timeProvider;
    private static ILogger<Win32DnsSdProvider> _logger = NullLogger<Win32DnsSdProvider>.Instance;
    private static Win32DnsSdProvider? _currentInstance;

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

    private readonly ConcurrentDictionary<string, DiscoveredService> _discoveredServices = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, nint> _activeBrowseRequests = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, nint> _activeBrowseCancels = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, nint> _activeResolveRequests = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, nint> _activeResolveCancels = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _resolveTimeouts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _discoveredTypes = new(StringComparer.OrdinalIgnoreCase);

    private readonly Lock _lock = new();

    public Win32DnsSdProvider(TimeProvider? timeProvider = null, ILogger<Win32DnsSdProvider>? logger = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<Win32DnsSdProvider>.Instance;
        _currentInstance = this;
    }

    public Task StartDiscoveryAsync(DiscoveryOptions options, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            StopInternal();

            lock (_lock)
            {
                _currentInstance = this;
                _discoveredServices.Clear();
                _discoveredTypes.Clear();
                State = DiscoveryState.Discovering;

                if (options.TargetServiceTypes != null && options.TargetServiceTypes.Count > 0)
                {
                    _logger.LogInformation("Starting Win32 DNS-SD discovery with {Count} targeted service types (InterfaceIndex=0)", options.TargetServiceTypes.Count);
                    foreach (var serviceType in options.TargetServiceTypes)
                    {
                        BrowseServiceType(serviceType);
                    }
                }
                else if (options.Mode == DiscoveryMode.Hybrid)
                {
                    _logger.LogInformation("Starting Win32 DNS-SD Hybrid discovery (Meta + Common scan types, InterfaceIndex=0)");
                    BrowseMetaServices();

                    foreach (var commonType in WellKnownServiceCatalog.CommonScanTypes)
                    {
                        if (_discoveredTypes.TryAdd(commonType, 0))
                        {
                            BrowseServiceType(commonType);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Starting Win32 DNS-SD Recursive Wildcard meta-discovery (InterfaceIndex=0)");
                    BrowseMetaServices();
                }
            }
        }, ct);
    }

    private unsafe void BrowseMetaServices()
    {
        const string key = "_services._dns-sd._udp.local";
        if (_activeBrowseCancels.ContainsKey(key))
            return;

        try
        {
            var pQueryName = Marshal.StringToHGlobalUni(key);
            var pReq = (DNS_SERVICE_BROWSE_REQUEST*)NativeMemory.AllocZeroed((nuint)sizeof(DNS_SERVICE_BROWSE_REQUEST));
            var pCancel = (DNS_SERVICE_CANCEL*)NativeMemory.AllocZeroed((nuint)sizeof(DNS_SERVICE_CANCEL));

            pReq->Version = (uint)DNS_QUERY_OPTIONS.DNS_QUERY_REQUEST_VERSION1;
            pReq->InterfaceIndex = 0; // Canonical 0 = all interfaces
            pReq->QueryName = new PCWSTR((char*)pQueryName);
            pReq->Anonymous.pBrowseCallback = &StaticMetaBrowseCallback;
            pReq->pQueryContext = null;

            _activeBrowseRequests[key] = (nint)pReq;
            _activeBrowseCancels[key] = (nint)pCancel;

            var dnsStatus = PInvoke.DnsServiceBrowse(in *pReq, ref *pCancel);
            if (dnsStatus != (int)WIN32_ERROR.NO_ERROR && dnsStatus != PInvoke.DNS_REQUEST_PENDING)
            {
                var hr = PInvoke.HRESULT_FROM_WIN32((WIN32_ERROR)dnsStatus);
                try
                {
                    hr.ThrowOnFailure();
                }
                catch (Win32Exception ex)
                {
                    _logger.LogWarning(
                        ex, "DnsServiceResolve for meta-services returned non-pending code: {ErrorCode} (0x{Hr:X8})",
                         dnsStatus, (uint)hr);
                }
            }
            else
            {
                _logger.LogDebug("DnsServiceResolve for meta-services scheduled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start meta-services browse");
        }
    }

    private unsafe void BrowseServiceType(string serviceType)
    {
        var cleanType = serviceType.Trim();
        if (!cleanType.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            cleanType += ".local";
        }

        if (_activeBrowseCancels.ContainsKey(cleanType))
            return;

        try
        {
            var pQueryName = Marshal.StringToHGlobalUni(cleanType);
            var pReq = (DNS_SERVICE_BROWSE_REQUEST*)NativeMemory.AllocZeroed((nuint)sizeof(DNS_SERVICE_BROWSE_REQUEST));
            var pCancel = (DNS_SERVICE_CANCEL*)NativeMemory.AllocZeroed((nuint)sizeof(DNS_SERVICE_CANCEL));

            pReq->Version = (uint)DNS_QUERY_OPTIONS.DNS_QUERY_REQUEST_VERSION1;
            pReq->InterfaceIndex = 0; // Canonical 0 = all interfaces
            pReq->QueryName = new PCWSTR((char*)pQueryName);
            pReq->Anonymous.pBrowseCallback = &StaticTypeBrowseCallback;
            pReq->pQueryContext = null;

            _activeBrowseRequests[cleanType] = (nint)pReq;
            _activeBrowseCancels[cleanType] = (nint)pCancel;

            var dnsStatus = PInvoke.DnsServiceBrowse(in *pReq, ref *pCancel);
            if (dnsStatus != (int)WIN32_ERROR.NO_ERROR && dnsStatus != PInvoke.DNS_REQUEST_PENDING)
            {
                var hr = PInvoke.HRESULT_FROM_WIN32((WIN32_ERROR)dnsStatus);
                try
                {
                    hr.ThrowOnFailure();
                }
                catch (Win32Exception ex)
                {
                    _logger.LogWarning(
                        ex, "DnsServiceResolve for '{ServiceType}' returned non-pending code: {ErrorCode} (0x{Hr:X8})",
                         serviceType, dnsStatus, (uint)hr);
                }
            }
            else
            {
                _logger.LogDebug("DnsServiceResolve for '{ServiceType}' scheduled", serviceType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse service type '{ServiceType}'", cleanType);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe void StaticMetaBrowseCallback(uint status, void* pQueryContext, DNS_RECORDW* pDnsRecord)
    {
        _currentInstance?.HandleMetaBrowseRecord(status, pQueryContext, pDnsRecord);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe void StaticTypeBrowseCallback(uint status, void* pQueryContext, DNS_RECORDW* pDnsRecord)
    {
        _currentInstance?.HandleTypeBrowseRecord(status, pQueryContext, pDnsRecord);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe void StaticResolveCallback(uint status, void* pQueryContext, DNS_SERVICE_INSTANCE* pInstance)
    {
        _currentInstance?.HandleResolvedInstance(status, pQueryContext, pInstance);
    }

    private unsafe void HandleMetaBrowseRecord(uint status, void* pQueryContext, DNS_RECORDW* pDnsRecord)
    {
        if (pDnsRecord == null)
            return;

        try
        {
            var curr = pDnsRecord;
            while (curr != null)
            {
                if (curr->wType == (ushort)PInvoke.DNS_TYPE_PTR)
                {
                    var pNameHost = curr->Data.PTR.pNameHost;
                    if (pNameHost.Value != null)
                    {
                        var serviceType = new string(pNameHost.Value);
                        if (!string.IsNullOrWhiteSpace(serviceType))
                        {
                            if (_discoveredTypes.TryAdd(serviceType, 0))
                            {
                                _logger.LogInformation("Discovered advertised DNS-SD service type: '{ServiceType}' -> Recursively scanning instances", serviceType);
                                BrowseServiceType(serviceType);
                            }

                            // If serviceType is a subtype (e.g., _rrkSD02jpiAwI4Qh._usp-agt-mqtt._tcp.local),
                            // also extract and browse the canonical base service type (e.g., _usp-agt-mqtt._tcp.local).
                            var labels = serviceType.TrimEnd('.').Split('.');
                            if (labels.Length >= 3)
                            {
                                var isLocal = labels[^1].Equals("local", StringComparison.OrdinalIgnoreCase);
                                var proto = isLocal && labels.Length >= 4 ? labels[^2] : labels[^1];
                                var baseName = isLocal && labels.Length >= 4 ? labels[^3] : labels[^2];
                                if (baseName.StartsWith('_') && (proto.Equals("_tcp", StringComparison.OrdinalIgnoreCase) || proto.Equals("_udp", StringComparison.OrdinalIgnoreCase)))
                                {
                                    var baseType = $"{baseName}.{proto}.local";
                                    if (_discoveredTypes.TryAdd(baseType, 0))
                                    {
                                        _logger.LogInformation("Discovered base DNS-SD service type from subtype: '{BaseType}'", baseType);
                                        BrowseServiceType(baseType);
                                    }
                                }
                            }
                        }
                    }
                }
                curr = curr->pNext;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing meta browse record");
        }
        finally
        {
            try
            {
                PInvoke.DnsFree(pDnsRecord, DNS_FREE_TYPE.DnsFreeRecordList);
            }
            catch { }
        }
    }

    private unsafe void HandleTypeBrowseRecord(uint status, void* pQueryContext, DNS_RECORDW* pDnsRecord)
    {
        if (pDnsRecord == null)
            return;

        try
        {
            var curr = pDnsRecord;
            while (curr != null)
            {
                if (curr->wType == (ushort)PInvoke.DNS_TYPE_PTR)
                {
                    var pNameHost = curr->Data.PTR.pNameHost;
                    if (pNameHost.Value != null)
                    {
                        var fullInstancePath = new string(pNameHost.Value);
                        if (!string.IsNullOrWhiteSpace(fullInstancePath))
                        {
                            var (instanceName, serviceType, domain) = ParseFullInstancePath(fullInstancePath);

                            // Process valid instance paths (ignore bare service types without an instance name)
                            if (!string.IsNullOrWhiteSpace(instanceName))
                            {
                                var serviceId = fullInstancePath;

                                // RFC 6762 Section 10.1: Goodbye packets send PTR with TTL = 0 when departing
                                if (curr->dwTtl == 0)
                                {
                                    if (_discoveredServices.TryGetValue(serviceId, out var existing))
                                    {
                                        existing.IsOnline = false;
                                        existing.LastSeen = (_currentInstance?._timeProvider ?? TimeProvider.System).GetUtcNow();
                                        _logger.LogInformation("Service departed network (Goodbye packet): '{InstancePath}'", fullInstancePath);
                                        ServiceLost?.Invoke(this, new ServiceLostEventArgs(serviceId));
                                    }
                                }
                                else
                                {
                                    var now = (_currentInstance?._timeProvider ?? TimeProvider.System).GetUtcNow();
                                    var initialService = new DiscoveredService
                                    {
                                        Id = serviceId,
                                        InstanceName = instanceName,
                                        ServiceType = serviceType,
                                        Domain = domain,
                                        ProviderId = "win32",
                                        IsOnline = true,
                                        FirstSeen = now,
                                        LastSeen = now
                                    };

                                    if (_discoveredServices.TryAdd(initialService.Id, initialService))
                                    {
                                        _logger.LogInformation("Discovered service instance: '{InstancePath}'", fullInstancePath);
                                        ServiceDiscovered?.Invoke(this, new ServiceDiscoveredEventArgs(initialService));
                                        ResolveServiceInstance(fullInstancePath);
                                    }
                                    else
                                    {
                                        if (_discoveredServices.TryGetValue(initialService.Id, out var existing))
                                        {
                                            existing.LastSeen = now;
                                            existing.IsOnline = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                curr = curr->pNext;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing type browse record");
        }
        finally
        {
            try
            {
                PInvoke.DnsFree(pDnsRecord, DNS_FREE_TYPE.DnsFreeRecordList);
            }
            catch { }
        }
    }

    private unsafe void CleanupResolveRequest(string fullInstancePath, bool cancelInFlight = true)
    {
        if (_resolveTimeouts.TryRemove(fullInstancePath, out var timeoutCts))
        {
            try
            {
                timeoutCts.Cancel();
                timeoutCts.Dispose();
            }
            catch { }
        }

        if (_activeResolveCancels.TryRemove(fullInstancePath, out var pCancelInt))
        {
            var pCancel = (DNS_SERVICE_CANCEL*)pCancelInt;
            try
            {
                if (cancelInFlight && pCancel != null)
                {
                    var dnsStatus = PInvoke.DnsServiceResolveCancel(in *pCancel);
                    var hr = PInvoke.HRESULT_FROM_WIN32((WIN32_ERROR)dnsStatus);
                    hr.ThrowOnFailure();
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Exception cancelling resolve for '{Instance}'", fullInstancePath);
            }
            finally
            {
                if (pCancel != null)
                {
                    NativeMemory.Free(pCancel);
                }
            }
        }

        if (_activeResolveRequests.TryRemove(fullInstancePath, out var pReqInt))
        {
            var pReq = (DNS_SERVICE_RESOLVE_REQUEST*)pReqInt;
            try
            {
                if (pReq != null && pReq->QueryName.Value != null)
                {
                    Marshal.FreeHGlobal((nint)pReq->QueryName.Value);
                }
            }
            finally
            {
                if (pReq != null)
                {
                    NativeMemory.Free(pReq);
                }
            }
        }
    }

    private void HandleResolutionTimeoutOrFailure(string fullInstancePath, ResolutionFailureReason reason, string? details = null)
    {
        CleanupResolveRequest(fullInstancePath);

        _ = Task.Run(async () =>
        {
            if (_discoveredServices.TryGetValue(fullInstancePath, out var svc))
            {
                if (svc.ResolutionStatus == ResolutionStatus.Resolved || svc.IPv4Addresses.Count > 0 || svc.IPv6Addresses.Count > 0)
                {
                    return;
                }

                if (svc.ResolutionStatus == ResolutionStatus.Resolving)
                {
                    // If host is not provided, try inferring from '@ <host>' in instance name (e.g. CUPS Bonjour printers)
                    if (string.IsNullOrWhiteSpace(svc.HostName) && svc.InstanceName.Contains('@'))
                    {
                        var atIdx = svc.InstanceName.LastIndexOf('@');
                        var hostCandidate = svc.InstanceName[(atIdx + 1)..].Trim();
                        if (!string.IsNullOrWhiteSpace(hostCandidate))
                        {
                            svc.HostName = hostCandidate.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
                                ? hostCandidate
                                : $"{hostCandidate}.local";
                            _logger.LogInformation("Inferred host '{Host}' from instance '{Instance}'", svc.HostName, svc.InstanceName);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(svc.HostName))
                    {
                        await ResolveIpAddressesAsync(svc);
                    }
                    else
                    {
                        svc.ResolutionStatus = ResolutionStatus.Failed;
                        svc.FailureReason = reason;
                        svc.FailureDetails = details;
                        ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(svc));
                    }
                }
            }
        });
    }

    private void ScheduleResolveTimeout(string fullInstancePath, CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_resolveTimeout, token);
                _logger.LogInformation("Resolution timed out for instance '{InstancePath}'", fullInstancePath);
                HandleResolutionTimeoutOrFailure(fullInstancePath, ResolutionFailureReason.Timeout, null);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error in resolve timeout watchdog for '{InstancePath}'", fullInstancePath);
            }
        }, token);
    }

    private unsafe void ResolveServiceInstance(string fullInstancePath)
    {
        if (_activeResolveCancels.ContainsKey(fullInstancePath))
            return;

        try
        {
            var pQueryName = Marshal.StringToHGlobalUni(fullInstancePath);
            var pReq = (DNS_SERVICE_RESOLVE_REQUEST*)NativeMemory.AllocZeroed((nuint)sizeof(DNS_SERVICE_RESOLVE_REQUEST));
            var pCancel = (DNS_SERVICE_CANCEL*)NativeMemory.AllocZeroed((nuint)sizeof(DNS_SERVICE_CANCEL));

            pReq->Version = (uint)DNS_QUERY_OPTIONS.DNS_QUERY_REQUEST_VERSION1;
            pReq->InterfaceIndex = 0; // Canonical 0 = all interfaces
            pReq->QueryName = new PWSTR((char*)pQueryName);
            pReq->pResolveCompletionCallback = &StaticResolveCallback;
            pReq->pQueryContext = null;

            _activeResolveRequests[fullInstancePath] = (nint)pReq;
            _activeResolveCancels[fullInstancePath] = (nint)pCancel;

            var cts = new CancellationTokenSource();
            _resolveTimeouts[fullInstancePath] = cts;
            ScheduleResolveTimeout(fullInstancePath, cts.Token);

            var dnsStatus = PInvoke.DnsServiceResolve(in *pReq, ref *pCancel);
            if (dnsStatus != (int)WIN32_ERROR.NO_ERROR && dnsStatus != PInvoke.DNS_REQUEST_PENDING)
            {
                var hr = PInvoke.HRESULT_FROM_WIN32((WIN32_ERROR)dnsStatus);
                try
                {
                    hr.ThrowOnFailure();
                }
                catch (Win32Exception ex)
                {
                    _logger.LogWarning(
                        ex, "DnsServiceResolve for '{InstancePath}' returned non-pending code: {ErrorCode} (0x{Hr:X8})",
                        fullInstancePath, dnsStatus, (uint)hr);
                    HandleResolutionTimeoutOrFailure(fullInstancePath, ResolutionFailureReason.DnsQueryFailed, ex.Message);
                }
            }
            else
            {
                _logger.LogDebug("DnsServiceResolve for '{InstancePath}' scheduled", fullInstancePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve instance '{InstancePath}'", fullInstancePath);
        }
    }

    private unsafe void HandleResolvedInstance(uint status, void* pQueryContext, DNS_SERVICE_INSTANCE* pInstance)
    {
        if (pInstance == null)
            return;

        try
        {
            if (pInstance->pszInstanceName.Value != null)
            {
                var fullPath = new string(pInstance->pszInstanceName.Value);
                CleanupResolveRequest(fullPath, cancelInFlight: false);
            }

            var service = ParseResolvedInstance(pInstance);
            if (service != null)
            {

                var isNew = _discoveredServices.TryAdd(service.Id, service);
                if (isNew)
                {
                    _logger.LogInformation("Discovered service [{ServiceType}]: '{InstanceName}' on host '{Host}' ({Endpoint}) via Win32",
                        service.ServiceType, service.InstanceName, service.HostName, service.FormattedEndpoint);
                    ServiceDiscovered?.Invoke(this, new ServiceDiscoveredEventArgs(service));
                    _ = Task.Run(() => ResolveIpAddressesAsync(service));
                }
                else
                {
                    var existing = _discoveredServices[service.Id];
                    var changed = false;
                    if (!string.IsNullOrWhiteSpace(service.HostName) && existing.HostName != service.HostName)
                    { existing.HostName = service.HostName; changed = true; }
                    if (service.Port.HasValue && existing.Port != service.Port)
                    { existing.Port = service.Port; changed = true; }
                    if (service.TxtRecords.Count > 0 && existing.TxtRecords.Count != service.TxtRecords.Count)
                    { existing.TxtRecords = service.TxtRecords; changed = true; }
                    foreach (var ip in service.IPv4Addresses)
                        if (!existing.IPv4Addresses.Contains(ip))
                        { existing.IPv4Addresses.Add(ip); changed = true; }
                    foreach (var ip in service.IPv6Addresses)
                        if (!existing.IPv6Addresses.Contains(ip))
                        { existing.IPv6Addresses.Add(ip); changed = true; }

                    if (service.IPv4Addresses.Count > 0 || service.IPv6Addresses.Count > 0)
                    {
                        existing.ResolutionStatus = ResolutionStatus.Resolved;
                        existing.FailureReason = ResolutionFailureReason.None;
                        existing.FailureDetails = null;
                        changed = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(service.HostName) && existing.ResolutionStatus == ResolutionStatus.Failed)
                    {
                        // Reset status back to Resolving to allow host address resolution to proceed
                        existing.ResolutionStatus = ResolutionStatus.Resolving;
                        existing.FailureReason = ResolutionFailureReason.None;
                        existing.FailureDetails = null;
                        changed = true;
                    }

                    existing.LastSeen = (_currentInstance?._timeProvider ?? TimeProvider.System).GetUtcNow();
                    existing.IsOnline = true;

                    if (changed)
                    {
                        ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(existing));
                        _ = Task.Run(() => ResolveIpAddressesAsync(existing));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling resolved DNS-SD instance");
        }
        finally
        {
            try
            {
                PInvoke.DnsServiceFreeInstance(pInstance);
            }
            catch { }
        }
    }

    private unsafe DiscoveredService? ParseResolvedInstance(DNS_SERVICE_INSTANCE* pInst)
    {
        if (pInst->pszInstanceName.Value == null)
            return null;

        var fullInstancePath = new string(pInst->pszInstanceName.Value);
        var hostName = pInst->pszHostName.Value != null ? new string(pInst->pszHostName.Value) : null;
        var (instanceName, serviceType, domain) = ParseFullInstancePath(fullInstancePath);

        var txtRecords = new List<TxtRecordItem>();
        if (pInst->dwPropertyCount > 0 && pInst->keys != null && pInst->values != null)
        {
            for (int i = 0; i < pInst->dwPropertyCount; i++)
            {
                var pKey = pInst->keys[i];
                var pVal = pInst->values[i];
                var k = pKey.Value != null ? new string(pKey.Value) : null;
                var v = pVal.Value != null ? new string(pVal.Value) : null;
                if (!string.IsNullOrWhiteSpace(k))
                {
                    txtRecords.Add(new TxtRecordItem(k, v ?? string.Empty));
                }
            }
        }

        var now = (_currentInstance?._timeProvider ?? TimeProvider.System).GetUtcNow();
        var service = new DiscoveredService
        {
            Id = fullInstancePath,
            InstanceName = instanceName,
            ServiceType = serviceType,
            Domain = domain,
            HostName = hostName,
            Port = pInst->wPort > 0 ? pInst->wPort : null,
            TxtRecords = txtRecords,
            ProviderId = "win32",
            IsOnline = true,
            FirstSeen = now,
            LastSeen = now,
            ResolutionStatus = ResolutionStatus.Resolving
        };

        if (pInst->ip4Address != null && IPAddressExtensions.IsValidIPv4(*pInst->ip4Address))
        {
            var ip4 = new IPAddress(*pInst->ip4Address);
            if (!service.IPv4Addresses.Contains(ip4))
            {
                service.IPv4Addresses.Add(ip4);
            }
        }

        if (pInst->ip6Address != null)
        {
            var span = new ReadOnlySpan<byte>(pInst->ip6Address, sizeof(IP6_ADDRESS));
            if (IPAddressExtensions.IsValidIPv6(span))
            {
                var ip6 = new IPAddress(span);
                if (!service.IPv6Addresses.Contains(ip6))
                {
                    service.IPv6Addresses.Add(ip6);
                }
            }
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

    private static (string InstanceName, string ServiceType, string Domain) ParseFullInstancePath(string fullPath)
    {
        var span = fullPath.AsSpan().TrimEnd('.');

        var lastDot = span.LastIndexOf('.');
        if (lastDot < 0)
            return (span.ToString(), "_unknown._tcp", "local");
        var domainSpan = span[(lastDot + 1)..];
        var rest1 = span[..lastDot];

        var secondDot = rest1.LastIndexOf('.');
        if (secondDot < 0)
            return (rest1.ToString(), "_unknown._tcp", domainSpan.ToString());
        var protoSpan = rest1[(secondDot + 1)..];
        var rest2 = rest1[..secondDot];

        var thirdDot = rest2.LastIndexOf('.');
        if (thirdDot < 0)
        {
            var typeOnly = rest2;
            return (string.Empty, $"{typeOnly}.{protoSpan}", domainSpan.ToString());
        }

        var typeSpan = rest2[(thirdDot + 1)..];
        var instanceSpan = rest2[..thirdDot];

        return (instanceSpan.ToString(), $"{typeSpan}.{protoSpan}", domainSpan.ToString());
    }

    private async Task ResolveIpAddressesAsync(DiscoveredService service)
    {
        var hadNativeAddresses = service.IPv4Addresses.Count > 0 || service.IPv6Addresses.Count > 0;

        if (string.IsNullOrWhiteSpace(service.HostName))
        {
            if (!hadNativeAddresses)
            {
                service.ResolutionStatus = ResolutionStatus.Failed;
                service.FailureReason = ResolutionFailureReason.NoHostName;
                service.FailureDetails = null;
                if (_discoveredServices.TryGetValue(service.Id, out var existingNoHost))
                {
                    existingNoHost.ResolutionStatus = service.ResolutionStatus;
                    existingNoHost.FailureReason = service.FailureReason;
                    existingNoHost.FailureDetails = service.FailureDetails;
                    ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(existingNoHost));
                }
            }
            return;
        }

        var result = await NativeDnsHostResolver.ResolveHostAddressesAsync(service.HostName).ConfigureAwait(false);

        foreach (var ip in result.IPv4Addresses)
        {
            if (!service.IPv4Addresses.Contains(ip))
            {
                service.IPv4Addresses.Add(ip);
            }
        }

        foreach (var ip in result.IPv6Addresses)
        {
            if (!service.IPv6Addresses.Contains(ip))
            {
                service.IPv6Addresses.Add(ip);
            }
        }

        service.IsFallbackResolution = !hadNativeAddresses && result.IsFallback;

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

        if (_discoveredServices.TryGetValue(service.Id, out var target))
        {
            target.IPv4Addresses = service.IPv4Addresses;
            target.IPv6Addresses = service.IPv6Addresses;
            target.ResolutionStatus = service.ResolutionStatus;
            target.FailureReason = service.FailureReason;
            target.FailureDetails = service.FailureDetails;
            target.IsFallbackResolution = service.IsFallbackResolution;
            ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(target));
        }
    }

    public async Task<DiscoveredService?> ResolveDetailsAsync(DiscoveredService service, CancellationToken ct = default)
    {
        service.ResolutionStatus = ResolutionStatus.Resolving;
        service.FailureReason = ResolutionFailureReason.None;
        service.FailureDetails = null;
        if (_discoveredServices.TryGetValue(service.Id, out var existing))
        {
            existing.ResolutionStatus = ResolutionStatus.Resolving;
            existing.FailureReason = ResolutionFailureReason.None;
            existing.FailureDetails = null;
            ServiceUpdated?.Invoke(this, new ServiceUpdatedEventArgs(existing));
        }

        CleanupResolveRequest(service.Id);

        // 1. Re-initiate full DNS-SD instance resolution to query SRV/TXT and discover host name
        ResolveServiceInstance(service.Id);

        // 2. If host name is already present, also resolve host IP addresses directly
        if (!string.IsNullOrWhiteSpace(service.HostName))
        {
            await ResolveIpAddressesAsync(service);
        }

        return service;
    }

    private unsafe void StopInternal()
    {
        lock (_lock)
        {
            foreach (var pCancelInt in _activeBrowseCancels.Values)
            {
                var pCancel = (DNS_SERVICE_CANCEL*)pCancelInt;
                try
                {
                    if (pCancel != null)
                    {
                        var dnsStatus = PInvoke.DnsServiceBrowseCancel(in *pCancel);
                        var hr = PInvoke.HRESULT_FROM_WIN32((WIN32_ERROR)dnsStatus);
                        hr.ThrowOnFailure();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Exception cancelling browse");
                }
                finally
                {
                    if (pCancel != null)
                    {
                        NativeMemory.Free(pCancel);
                    }
                }
            }
            _activeBrowseCancels.Clear();

            foreach (var pReqInt in _activeBrowseRequests.Values)
            {
                var pReq = (DNS_SERVICE_BROWSE_REQUEST*)pReqInt;
                if (pReq != null)
                {
                    if (pReq->QueryName.Value != null)
                    {
                        Marshal.FreeHGlobal((nint)pReq->QueryName.Value);
                    }
                    NativeMemory.Free(pReq);
                }
            }
            _activeBrowseRequests.Clear();

            foreach (var pCancelInt in _activeResolveCancels.Values)
            {
                var pCancel = (DNS_SERVICE_CANCEL*)pCancelInt;
                try
                {
                    if (pCancel != null)
                    {
                        var dnsStatus = PInvoke.DnsServiceResolveCancel(in *pCancel);
                        var hr = PInvoke.HRESULT_FROM_WIN32((WIN32_ERROR)dnsStatus);
                        hr.ThrowOnFailure();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Exception cancelling resolve");
                }
                finally
                {
                    if (pCancel != null)
                    {
                        NativeMemory.Free(pCancel);
                    }
                }
            }
            _activeResolveCancels.Clear();

            foreach (var pReqInt in _activeResolveRequests.Values)
            {
                var pReq = (DNS_SERVICE_RESOLVE_REQUEST*)pReqInt;
                if (pReq != null)
                {
                    if (pReq->QueryName.Value != null)
                    {
                        Marshal.FreeHGlobal((nint)pReq->QueryName.Value);
                    }
                    NativeMemory.Free(pReq);
                }
            }
            _activeResolveRequests.Clear();

            foreach (var timeout in _resolveTimeouts.Values)
            {
                try
                { timeout.Cancel(); timeout.Dispose(); }
                catch { }
            }
            _resolveTimeouts.Clear();

            State = DiscoveryState.Idle;
        }
    }

    public Task StopDiscoveryAsync()
    {
        return Task.Run(() =>
        {
            StopInternal();
            _logger.LogInformation("Win32 DNS-SD discovery stopped");
        });
    }

    public async ValueTask DisposeAsync()
    {
        await StopDiscoveryAsync();
        GC.SuppressFinalize(this);
    }
}
