using System.Text.Json.Serialization;
using mDiscover.Core.Models;

namespace mDiscover.Core.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = [typeof(IPAddressJsonConverter)])]
[JsonSerializable(typeof(DiscoveredService))]
[JsonSerializable(typeof(List<DiscoveredService>))]
[JsonSerializable(typeof(TxtRecordItem))]
[JsonSerializable(typeof(List<TxtRecordItem>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(System.Net.IPAddress))]
[JsonSerializable(typeof(List<System.Net.IPAddress>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
