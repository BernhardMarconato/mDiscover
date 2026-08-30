namespace mDiscover.Core.Extensions;

/// <summary>
/// Extension methods for property dictionaries and device information attribute lookups.
/// </summary>
public static class PropertyExtensions
{
    extension(IReadOnlyDictionary<string, object>? properties)
    {
        /// <summary>
        /// Attempts to retrieve a strongly-typed value from a property dictionary by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="key">The property key to look up.</param>
        /// <returns>The typed value if present and of type <typeparamref name="T"/>; otherwise default.</returns>
        public T? TryGetProperty<T>(string key)
        {
            if (properties != null && properties.TryGetValue(key, out var val) && val is T typedVal)
            {
                return typedVal;
            }

            return default;
        }
    }
}

