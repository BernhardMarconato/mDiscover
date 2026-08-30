using System.Net;

namespace mDiscover.Core.Models;

/// <summary>
/// Represents the endpoint resolution status of a discovered service.
/// </summary>
public enum ResolutionStatus
{
    /// <summary>
    /// Hostname, port, and IP address queries are currently in progress.
    /// </summary>
    Resolving = 0,

    /// <summary>
    /// Hostname, IP addresses, and TXT records were successfully resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// Service resolution failed or timed out.
    /// </summary>
    Failed
}

/// <summary>
/// Defines failure classifications for service host and endpoint resolution.
/// </summary>
public enum ResolutionFailureReason
{
    /// <summary>
    /// No failure occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The DNS-SD record did not contain a target host name.
    /// </summary>
    NoHostName,

    /// <summary>
    /// No DNS SRV records were returned for the service.
    /// </summary>
    NoSrvRecords,

    /// <summary>
    /// Hostname resolution returned zero IPv4 and IPv6 addresses.
    /// </summary>
    NoAddressesFound,

    /// <summary>
    /// The underlying Win32 / WinRT DNS query failed.
    /// </summary>
    DnsQueryFailed,

    /// <summary>
    /// Resolution exceeded the allowable timeout duration.
    /// </summary>
    Timeout,

    /// <summary>
    /// A generic or unspecified resolution error occurred.
    /// </summary>
    GenericFailure
}

/// <summary>
/// Encapsulates resolved IPv4/IPv6 addresses and diagnostic error details for a hostname query.
/// </summary>
/// <param name="IPv4Addresses">The list of resolved IPv4 addresses.</param>
/// <param name="IPv6Addresses">The list of resolved IPv6 addresses.</param>
/// <param name="FailureReason">The failure classification reason if resolution was unsuccessful.</param>
/// <param name="FailureDetails">Optional diagnostic message or system error description.</param>
/// <param name="IsFallback">A value indicating whether fallback system resolution was used.</param>
public record HostResolutionResult(
    IReadOnlyList<IPAddress> IPv4Addresses,
    IReadOnlyList<IPAddress> IPv6Addresses,
    ResolutionFailureReason FailureReason,
    string? FailureDetails,
    bool IsFallback = false
);

