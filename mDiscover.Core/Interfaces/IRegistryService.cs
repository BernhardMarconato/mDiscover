using Microsoft.Win32;

namespace mDiscover.Core.Interfaces;

/// <summary>
/// Provides an abstraction over Windows registry access to allow testing without interacting with the real system registry.
/// </summary>
public interface IRegistryService
{
    /// <summary>
    /// Retrieves the raw value associated with the specified name from a registry subkey under the given hive.
    /// </summary>
    /// <param name="hive">The registry hive to query (e.g. <see cref="RegistryHive.LocalMachine"/> or <see cref="RegistryHive.CurrentUser"/>).</param>
    /// <param name="subKey">The path or name of the subkey.</param>
    /// <param name="valueName">The name of the value to retrieve.</param>
    /// <returns>The raw value associated with <paramref name="valueName"/>, or <see langword="null"/> if the key or value does not exist.</returns>
    object? GetValue(RegistryHive hive, string subKey, string valueName);

    /// <summary>
    /// Retrieves a 32-bit integer value associated with the specified name, parsing numeric strings or converting integer types as needed.
    /// </summary>
    /// <param name="hive">The registry hive to query.</param>
    /// <param name="subKey">The path or name of the subkey.</param>
    /// <param name="valueName">The name of the value to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key, value, or valid integer does not exist.</param>
    /// <returns>The parsed integer value, or <paramref name="defaultValue"/> if not found or invalid.</returns>
    int? GetInt32(RegistryHive hive, string subKey, string valueName, int? defaultValue = null);

    /// <summary>
    /// Retrieves a string value associated with the specified name.
    /// </summary>
    /// <param name="hive">The registry hive to query.</param>
    /// <param name="subKey">The path or name of the subkey.</param>
    /// <param name="valueName">The name of the value to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or value does not exist.</param>
    /// <returns>The string value, or <paramref name="defaultValue"/> if not found.</returns>
    string? GetString(RegistryHive hive, string subKey, string valueName, string? defaultValue = null);
}

