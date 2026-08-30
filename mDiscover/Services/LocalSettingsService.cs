using System.Text.Json;
using Microsoft.Windows.Storage;
using Microsoft.Extensions.Logging;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Serialization;

namespace mDiscover.Services;

public class LocalSettingsService(ILogger<LocalSettingsService> logger, ApplicationData appData) : ISettingsService
{
    private readonly ILogger<LocalSettingsService> _logger = logger;
    private readonly ApplicationDataContainer? _localSettings = appData.LocalSettings;

    public T ReadSetting<T>(SettingDefinition<T> setting)
    {
        try
        {
            if (_localSettings != null && _localSettings.Values.TryGetValue(setting.Key, out var val))
            {
                if (val is T direct)
                    return direct;

                if (typeof(T).IsEnum)
                {
                    if (val is string s && Enum.TryParse(typeof(T), s, true, out var parsedEnum))
                        return (T)parsedEnum;
                    if (val is int i)
                        return (T)Enum.ToObject(typeof(T), i);
                }

                if (typeof(T) == typeof(double))
                {
                    if (val is int i)
                        return (T)(object)(double)i;
                    if (val is float f)
                        return (T)(object)(double)f;
                    if (val is string s && double.TryParse(s, out var d))
                        return (T)(object)d;
                }

                if (typeof(T) == typeof(int))
                {
                    if (val is double d)
                        return (T)(object)(int)d;
                    if (val is string s && int.TryParse(s, out var i))
                        return (T)(object)i;
                }

                if (typeof(T) == typeof(bool))
                {
                    if (val is string s && bool.TryParse(s, out var b))
                        return (T)(object)b;
                }

                if (typeof(T) == typeof(string) && val != null)
                {
                    return (T)(object)val.ToString()!;
                }

                if (typeof(T) == typeof(List<string>) && val is string jsonStr)
                {
                    var list = JsonSerializer.Deserialize(jsonStr, AppJsonSerializerContext.Default.ListString);
                    if (list != null)
                    {
                        if (setting.Key == SettingDefinitions.EnabledServiceTypes.Key && list.Count == 0)
                        {
                            return (T)(object)WellKnownServiceCatalog.CommonScanTypes.ToList();
                        }
                        return (T)(object)list;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read setting '{Key}'", setting.Key);
        }

        if (setting.Key == SettingDefinitions.EnabledServiceTypes.Key && setting.DefaultValue is List<string> defList && defList.Count == 0)
        {
            return (T)(object)WellKnownServiceCatalog.CommonScanTypes.ToList();
        }

        return setting.DefaultValue;
    }

    public void SaveSetting<T>(SettingDefinition<T> setting, T value)
    {
        try
        {
            if (_localSettings != null)
            {
                if (typeof(T).IsEnum)
                {
                    _localSettings.Values[setting.Key] = value?.ToString();
                }
                else if (typeof(T) == typeof(List<string>) && value is List<string> list)
                {
                    _localSettings.Values[setting.Key] = JsonSerializer.Serialize(list, AppJsonSerializerContext.Default.ListString);
                }
                else
                {
                    _localSettings.Values[setting.Key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save setting '{Key}'", setting.Key);
        }
    }
}
