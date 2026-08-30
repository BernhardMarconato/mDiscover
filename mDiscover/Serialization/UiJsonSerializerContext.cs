using System.Text.Json.Serialization;
using mDiscover.Models;

namespace mDiscover.Serialization;

/// <summary>
/// Source-generated JSON serialization context for UI and presentation models.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WindowPlacement))]
public partial class UiJsonSerializerContext : JsonSerializerContext
{
}

