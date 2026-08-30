using System.Text;
using mDiscover.Core.Models;

namespace mDiscover.Core.Common;

/// <summary>
/// Provides utility methods to parse DNS-SD TXT record attributes according to RFC 6763.
/// Handles raw length-prefixed bytes, string arrays, key-value pairs, and boolean flags.
/// </summary>
public static class TxtRecordParser
{
    /// <summary>
    /// Parses a single TXT attribute entry (e.g. "key=value" or boolean flag "key").
    /// </summary>
    /// <param name="entry">The raw string attribute entry.</param>
    /// <returns>A parsed <see cref="TxtRecordItem"/>, or null if the entry is empty or invalid.</returns>
    public static TxtRecordItem? ParseEntry(string? entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            return null;

        var span = entry.AsSpan();
        var eqIdx = span.IndexOf('=');
        if (eqIdx >= 0)
        {
            var key = span[..eqIdx].Trim().ToString();
            var val = span[(eqIdx + 1)..].Trim().ToString();
            return string.IsNullOrEmpty(key) ? null : new TxtRecordItem(key, val);
        }

        var trimmed = entry.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : new TxtRecordItem(trimmed, "true");
    }

    /// <summary>
    /// Parses an enumerable collection of string TXT attribute entries into a list of <see cref="TxtRecordItem"/>.
    /// </summary>
    /// <param name="entries">The collection of attribute strings.</param>
    /// <returns>A list of parsed <see cref="TxtRecordItem"/> objects.</returns>
    public static List<TxtRecordItem> Parse(IEnumerable<string>? entries)
    {
        if (entries == null)
            return [];

        var result = new List<TxtRecordItem>();
        foreach (var entry in entries)
        {
            var item = ParseEntry(entry);
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// Parses RFC 6763 length-prefixed DNS-SD TXT record byte payload.
    /// Each entry is encoded as [1-byte length][key=value bytes].
    /// </summary>
    /// <param name="rawBytes">The byte span containing the packed TXT records.</param>
    /// <returns>A list of parsed <see cref="TxtRecordItem"/> objects.</returns>
    public static List<TxtRecordItem> ParseRfc6763Bytes(ReadOnlySpan<byte> rawBytes)
    {
        if (rawBytes.IsEmpty)
            return [];

        var result = new List<TxtRecordItem>();
        var span = rawBytes;

        while (!span.IsEmpty)
        {
            int len = span[0];
            span = span[1..];
            if (len == 0 || len > span.Length)
                break;

            var entrySpan = span[..len];
            span = span[len..];

            var eqIdx = entrySpan.IndexOf((byte)'=');
            if (eqIdx >= 0)
            {
                var key = Encoding.UTF8.GetString(entrySpan[..eqIdx]).Trim();
                var val = Encoding.UTF8.GetString(entrySpan[(eqIdx + 1)..]).Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    result.Add(new TxtRecordItem(key, val));
                }
            }
            else
            {
                var key = Encoding.UTF8.GetString(entrySpan).Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    result.Add(new TxtRecordItem(key, "true"));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Dynamically parses TXT attributes from an unknown object payload (e.g. string[], IEnumerable{string}, byte[], or string).
    /// </summary>
    /// <param name="rawProperty">The raw TXT property value.</param>
    /// <returns>A list of parsed <see cref="TxtRecordItem"/> objects.</returns>
    public static List<TxtRecordItem> ParseFromObject(object? rawProperty)
    {
        if (rawProperty == null)
            return [];

        if (rawProperty is IEnumerable<string> stringList)
            return Parse(stringList);

        if (rawProperty is byte[] bytes)
            return ParseRfc6763Bytes(bytes);

        if (rawProperty is string singleStr)
        {
            var item = ParseEntry(singleStr);
            return item != null ? [item] : [];
        }

        return [];
    }
}

