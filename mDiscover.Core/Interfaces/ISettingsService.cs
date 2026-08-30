using mDiscover.Core.Models;

namespace mDiscover.Core.Interfaces;

/// <summary>
/// Provides strongly-typed application configuration reading, persistence, and default value fallback.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Reads the current value for the specified setting definition, or its default value if not set.
    /// </summary>
    /// <typeparam name="T">The setting value type.</typeparam>
    /// <param name="setting">The strongly-typed setting definition.</param>
    /// <returns>The persisted setting value, or the default value.</returns>
    T ReadSetting<T>(SettingDefinition<T> setting);

    /// <summary>
    /// Persists the specified setting value.
    /// </summary>
    /// <typeparam name="T">The setting value type.</typeparam>
    /// <param name="setting">The strongly-typed setting definition.</param>
    /// <param name="value">The value to persist.</param>
    void SaveSetting<T>(SettingDefinition<T> setting, T value);
}

