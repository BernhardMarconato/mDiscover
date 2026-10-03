using Microsoft.Win32;
using mDiscover.Core.Interfaces;

namespace mDiscover.Core.Helpers;

/// <summary>
/// Default implementation of <see cref="IRegistryService"/> accessing the live Windows Registry.
/// </summary>
public sealed class WindowsRegistryService : IRegistryService
{
    public static WindowsRegistryService Instance { get; } = new();

    public object? GetValue(RegistryHive hive, string subKey, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var sub = baseKey.OpenSubKey(subKey);
            return sub?.GetValue(valueName);
        }
        catch
        {
            return null;
        }
    }

    public int? GetInt32(RegistryHive hive, string subKey, string valueName, int? defaultValue = null)
    {
        var raw = GetValue(hive, subKey, valueName);
        return raw switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    public string? GetString(RegistryHive hive, string subKey, string valueName, string? defaultValue = null)
    {
        var raw = GetValue(hive, subKey, valueName);
        return raw?.ToString() ?? defaultValue;
    }
}

