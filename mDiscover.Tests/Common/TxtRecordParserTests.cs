using System.Text;
using mDiscover.Core.Common;

namespace mDiscover.Tests.Common;

public class TxtRecordParserTests
{
    [Fact]
    public void ParseEntry_WithStandardKeyValue_ReturnsKeyAndValue()
    {
        var item = TxtRecordParser.ParseEntry("model=LCT001");
        Assert.NotNull(item);
        Assert.Equal("model", item.Key);
        Assert.Equal("LCT001", item.Value);
    }

    [Fact]
    public void ParseEntry_WithBooleanFlag_ReturnsValueTrue()
    {
        var item = TxtRecordParser.ParseEntry("paperless");
        Assert.NotNull(item);
        Assert.Equal("paperless", item.Key);
        Assert.Equal("true", item.Value);
    }

    [Fact]
    public void ParseEntry_WithEmptyValue_ReturnsEmptyStringValue()
    {
        var item = TxtRecordParser.ParseEntry("flag=");
        Assert.NotNull(item);
        Assert.Equal("flag", item.Key);
        Assert.Equal(string.Empty, item.Value);
    }

    [Fact]
    public void ParseEntry_WithValueContainingEquals_PreservesEntireValue()
    {
        var item = TxtRecordParser.ParseEntry("url=https://example.com/api?a=1&b=2");
        Assert.NotNull(item);
        Assert.Equal("url", item.Key);
        Assert.Equal("https://example.com/api?a=1&b=2", item.Value);
    }

    [Fact]
    public void ParseEntry_WithWhitespaceAroundKeyAndValue_TrimsCorrectly()
    {
        var item = TxtRecordParser.ParseEntry("  device_id = 98765  ");
        Assert.NotNull(item);
        Assert.Equal("device_id", item.Key);
        Assert.Equal("98765", item.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("=")]
    public void ParseEntry_WithNullOrInvalid_ReturnsNull(string? input)
    {
        var item = TxtRecordParser.ParseEntry(input);
        Assert.Null(item);
    }

    [Fact]
    public void Parse_WithListOfStrings_ParsesAllEntries()
    {
        var entries = new[] { "model=DeskJet", "color=1", "duplex", "   ", "version=2.0" };
        var results = TxtRecordParser.Parse(entries);

        Assert.Equal(4, results.Count);
        Assert.Equal("model", results[0].Key);
        Assert.Equal("DeskJet", results[0].Value);
        Assert.Equal("color", results[1].Key);
        Assert.Equal("1", results[1].Value);
        Assert.Equal("duplex", results[2].Key);
        Assert.Equal("true", results[2].Value);
        Assert.Equal("version", results[3].Key);
        Assert.Equal("2.0", results[3].Value);
    }

    [Fact]
    public void Parse_WithNull_ReturnsEmptyList()
    {
        var results = TxtRecordParser.Parse(null);
        Assert.Empty(results);
    }

    [Fact]
    public void ParseRfc6763Bytes_WithValidPackedEntries_ParsesSuccessfully()
    {
        // Construct [len][bytes][len][bytes]
        var e1 = Encoding.UTF8.GetBytes("txtvers=1");
        var e2 = Encoding.UTF8.GetBytes("note=Kitchen Speaker");
        var e3 = Encoding.UTF8.GetBytes("airplay");

        var stream = new List<byte>();
        stream.Add((byte)e1.Length);
        stream.AddRange(e1);
        stream.Add((byte)e2.Length);
        stream.AddRange(e2);
        stream.Add((byte)e3.Length);
        stream.AddRange(e3);

        var results = TxtRecordParser.ParseRfc6763Bytes(stream.ToArray());

        Assert.Equal(3, results.Count);
        Assert.Equal("txtvers", results[0].Key);
        Assert.Equal("1", results[0].Value);
        Assert.Equal("note", results[1].Key);
        Assert.Equal("Kitchen Speaker", results[1].Value);
        Assert.Equal("airplay", results[2].Key);
        Assert.Equal("true", results[2].Value);
    }

    [Fact]
    public void ParseRfc6763Bytes_WithTruncatedBuffer_HandlesSafelyWithoutThrowing()
    {
        // Declares length 50, but only 3 bytes follow
        byte[] malformed = [50, (byte)'a', (byte)'=', (byte)'b'];
        var results = TxtRecordParser.ParseRfc6763Bytes(malformed);
        Assert.Empty(results);
    }

    [Fact]
    public void ParseFromObject_WithVariousSupportedTypes_ExtractsRecords()
    {
        var listResult = TxtRecordParser.ParseFromObject(new List<string> { "foo=bar" });
        Assert.Single(listResult);
        Assert.Equal("foo", listResult[0].Key);
        Assert.Equal("bar", listResult[0].Value);

        var singleResult = TxtRecordParser.ParseFromObject("single=val");
        Assert.Single(singleResult);
        Assert.Equal("single", singleResult[0].Key);

        var nullResult = TxtRecordParser.ParseFromObject(null);
        Assert.Empty(nullResult);
    }
}

