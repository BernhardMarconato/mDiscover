using Microsoft.Win32;
using mDiscover.Core.Interfaces;

namespace mDiscover.Core.Helpers;

/// <summary>
/// Provides utility methods to inspect Windows DNS Client (Dnscache) registry configuration and diagnose mDNS availability.
/// </summary>
public static class DnsCacheRegistryHelper
{
    public const string DnscacheParametersSubKey = @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters";
    public const string EnableMdnsValueName = "EnableMDNS";

    /// <summary>
    /// Determines whether mDNS is explicitly disabled in the Windows registry under
    /// <c>SYSTEM\CurrentControlSet\Services\Dnscache\Parameters</c> (where <c>EnableMDNS</c> is 0).
    /// </summary>
    /// <param name="registryService">Optional registry abstraction. Defaults to live system registry via <see cref="WindowsRegistryService.Instance"/>.</param>
    /// <returns><see langword="true"/> if the value exists and is set to 0; otherwise, <see langword="false"/>.</returns>
    public static bool IsMdnsDisabledInRegistry(IRegistryService? registryService = null)
    {
        try
        {
            var registry = registryService ?? WindowsRegistryService.Instance;
            var value = registry.GetInt32(RegistryHive.LocalMachine, DnscacheParametersSubKey, EnableMdnsValueName);
            return value == 0;
        }
        catch
        {
            return false;
        }
    }
}

