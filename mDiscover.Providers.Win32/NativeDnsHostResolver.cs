using System.Net;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.Dns;
using mDiscover.Core.Extensions;
using mDiscover.Core.Models;
using mDiscover.Core.Services;
using mDiscover.Providers.Win32.Extensions;

namespace mDiscover.Providers.Win32;

/// <summary>
/// Provides Windows native DNS resolution for mDNS A/AAAA records using <c>PInvoke.DnsQuery_W</c> with system resolver fallback.
/// </summary>
internal static class NativeDnsHostResolver
{
    private const uint DNS_QUERY_MULTICAST_WAIT = 0x00040000;
    private const uint DNS_QUERY_STANDARD = 0x00000000;

    /// <summary>
    /// Asynchronously queries native Windows DNS APIs for mDNS A/AAAA records with managed fallback.
    /// </summary>
    internal static async Task<HostResolutionResult> ResolveHostAddressesAsync(string hostName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.NoHostName, null);
        }

        var cleanHost = hostName.Trim().TrimEnd('.');
        var ipv4 = new List<IPAddress>();
        var ipv6 = new List<IPAddress>();

        var (hrA, hrAAAA) = await Task.Run(() => QueryNativeDnsRecords(cleanHost, ipv4, ipv6), ct).ConfigureAwait(false);

        if (ipv4.Count > 0 || ipv6.Count > 0)
        {
            return new HostResolutionResult(ipv4, ipv6, ResolutionFailureReason.None, null, IsFallback: false);
        }

        // Fallback to system getaddrinfo (the same resolver stack used by ping / BCL)
        try
        {
            var fallbackResult = await DnsHostResolver.ResolveHostAddressesAsync(cleanHost, ct).ConfigureAwait(false);
            if (fallbackResult.IPv4Addresses.Count > 0 || fallbackResult.IPv6Addresses.Count > 0)
            {
                return new HostResolutionResult(fallbackResult.IPv4Addresses, fallbackResult.IPv6Addresses, ResolutionFailureReason.None, null, IsFallback: true);
            }
        }
        catch { }

        if (hrA == WIN32_ERROR.ERROR_TIMEOUT || hrAAAA == WIN32_ERROR.ERROR_TIMEOUT)
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.Timeout, null);
        }

        if (hrA != WIN32_ERROR.NO_ERROR && hrA != (WIN32_ERROR)PInvoke.DNS_INFO_NO_RECORDS)
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.DnsQueryFailed, hrA.ToFormattedString());
        }

        if (hrAAAA != WIN32_ERROR.NO_ERROR && hrAAAA != (WIN32_ERROR)PInvoke.DNS_INFO_NO_RECORDS)
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.DnsQueryFailed, hrAAAA.ToFormattedString());
        }

        return new HostResolutionResult([], [], ResolutionFailureReason.NoAddressesFound, null);
    }

    private static unsafe (WIN32_ERROR hrA, WIN32_ERROR hrAAAA) QueryNativeDnsRecords(string cleanHost, List<IPAddress> ipv4, List<IPAddress> ipv6)
    {
        var hrA = PInvoke.DnsQuery_W(
            cleanHost,
            (ushort)PInvoke.DNS_TYPE_A,
            (DNS_QUERY_OPTIONS)(DNS_QUERY_MULTICAST_WAIT | DNS_QUERY_STANDARD),
            out var pRecordRawA);

        var pRecordA = (DNS_RECORDW*)pRecordRawA;
        if (hrA == WIN32_ERROR.NO_ERROR && pRecordA != null)
        {
            for (var cur = pRecordA; cur != null; cur = cur->pNext)
            {
                if (cur->wType == (ushort)PInvoke.DNS_TYPE_A)
                {
                    if (IPAddressExtensions.IsValidIPv4(cur->Data.A.IpAddress))
                    {
                        var ip = new IPAddress(cur->Data.A.IpAddress);
                        if (!ipv4.Contains(ip))
                        {
                            ipv4.Add(ip);
                        }
                    }
                }
            }
            PInvoke.DnsFree(pRecordA, DNS_FREE_TYPE.DnsFreeRecordList);
        }

        var hrAAAA = PInvoke.DnsQuery_W(
            cleanHost,
            (ushort)PInvoke.DNS_TYPE_AAAA,
            (DNS_QUERY_OPTIONS)(DNS_QUERY_MULTICAST_WAIT | DNS_QUERY_STANDARD),
            out var pRecordRawAAAA);

        var pRecordAAAA = (DNS_RECORDW*)pRecordRawAAAA;
        if (hrAAAA == WIN32_ERROR.NO_ERROR && pRecordAAAA != null)
        {
            for (var cur = pRecordAAAA; cur != null; cur = cur->pNext)
            {
                if (cur->wType == (ushort)PInvoke.DNS_TYPE_AAAA)
                {
                    var span = new ReadOnlySpan<byte>((byte*)&cur->Data.AAAA.Ip6Address, sizeof(IP6_ADDRESS));
                    var ip = new IPAddress(span);
                    if (ip.IsValidHostAddress() && !ipv6.Contains(ip))
                    {
                        ipv6.Add(ip);
                    }
                }
            }
            PInvoke.DnsFree(pRecordAAAA, DNS_FREE_TYPE.DnsFreeRecordList);
        }

        return (hrA, hrAAAA);
    }
}
