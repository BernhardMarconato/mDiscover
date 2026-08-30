using System.Net;
using System.Net.Sockets;
using mDiscover.Core.Extensions;
using mDiscover.Core.Models;

namespace mDiscover.Core.Services;

/// <summary>
/// Provides asynchronous DNS host resolution using BCL networking APIs with timeouts and typed failure reporting.
/// </summary>
public static class DnsHostResolver
{
    /// <summary>
    /// Asynchronously resolves IPv4 and IPv6 addresses for a target hostname.
    /// </summary>
    public static async Task<HostResolutionResult> ResolveHostAddressesAsync(string hostName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.NoHostName, null);
        }

        var cleanHost = hostName.Trim().TrimEnd('.');
        var ipv4 = new List<IPAddress>();
        var ipv6 = new List<IPAddress>();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            var addresses = await Dns.GetHostAddressesAsync(cleanHost, cts.Token).ConfigureAwait(false);
            foreach (var addr in addresses)
            {
                if (!addr.IsValidHostAddress())
                    continue;

                if (addr.AddressFamily == AddressFamily.InterNetwork && !ipv4.Contains(addr))
                {
                    ipv4.Add(addr);
                }
                else if (addr.AddressFamily == AddressFamily.InterNetworkV6 && !ipv6.Contains(addr))
                {
                    ipv6.Add(addr);
                }
            }

            if (ipv4.Count > 0 || ipv6.Count > 0)
            {
                return new HostResolutionResult(ipv4, ipv6, ResolutionFailureReason.None, null);
            }

            return new HostResolutionResult([], [], ResolutionFailureReason.NoAddressesFound, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.Timeout, null);
        }
        catch (Exception ex)
        {
            return new HostResolutionResult([], [], ResolutionFailureReason.DnsQueryFailed, ex.Message);
        }
    }
}
