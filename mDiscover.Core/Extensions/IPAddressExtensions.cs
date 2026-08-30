namespace mDiscover.Core.Extensions;

using System.Net;

/// <summary>
/// Extension methods for <see cref="IPAddress"/> and raw network address structures.
/// </summary>
public static class IPAddressExtensions
{
    extension(IPAddress ip)
    {
        /// <summary>
        /// Determines whether the specified <see cref="IPAddress"/> is an unspecified/any address (0.0.0.0 or ::).
        /// </summary>
        public bool IsAny()
        {
            ArgumentNullException.ThrowIfNull(ip);
            return IPAddress.Any.Equals(ip) || IPAddress.IPv6Any.Equals(ip);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IPAddress"/> is a broadcast/none address (255.255.255.255 or ::).
        /// </summary>
        public bool IsNone()
        {
            ArgumentNullException.ThrowIfNull(ip);
            return IPAddress.None.Equals(ip) || IPAddress.IPv6None.Equals(ip);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IPAddress"/> represents a valid, non-zero, routable host address.
        /// Returns <see langword="false"/> for 0.0.0.0, 255.255.255.255, ::, or unspecified addresses.
        /// </summary>
        public bool IsValidHostAddress()
        {
            ArgumentNullException.ThrowIfNull(ip);
            return !ip.IsAny() && !ip.IsNone();
        }
    }

    /// <summary>
    /// Determines whether a raw 32-bit IPv4 address (in network byte order) is a valid, non-zero host address.
    /// </summary>
    public static bool IsValidIPv4(uint rawAddress)
    {
        return rawAddress != 0 && rawAddress != uint.MaxValue;
    }

    /// <summary>
    /// Determines whether a raw 128-bit IPv6 byte span represents a valid, non-zero host address.
    /// </summary>
    public static bool IsValidIPv6(ReadOnlySpan<byte> rawAddress)
    {
        if (rawAddress.Length < 16)
            return false;

        var isAllZero = true;
        for (var i = 0; i < 16; i++)
        {
            if (rawAddress[i] != 0)
            {
                isAllZero = false;
                break;
            }
        }

        return !isAllZero;
    }
}
