using mDiscover.Core.Extensions;

namespace mDiscover.Tests.Extensions;

public class PropertyExtensionsTests
{
    [Fact]
    public void TryGetProperty_WithExistingMatchingType_ReturnsTypedValue()
    {
        var dict = new Dictionary<string, object>
        {
            ["name"] = "Living Room Apple TV",
            ["port"] = 7000
        };

        var name = dict.TryGetProperty<string>("name");
        var port = dict.TryGetProperty<int>("port");

        Assert.Equal("Living Room Apple TV", name);
        Assert.Equal(7000, port);
    }

    [Fact]
    public void TryGetProperty_WithMissingKey_ReturnsDefault()
    {
        var dict = new Dictionary<string, object>
        {
            ["foo"] = "bar"
        };

        var missingStr = dict.TryGetProperty<string>("nonexistent");
        var missingInt = dict.TryGetProperty<int?>("nonexistent");

        Assert.Null(missingStr);
        Assert.Null(missingInt);
    }

    [Fact]
    public void TryGetProperty_WithMismatchedType_ReturnsDefault()
    {
        var dict = new Dictionary<string, object>
        {
            ["port"] = "not-a-number"
        };

        var port = dict.TryGetProperty<int?>("port");
        Assert.Null(port);
    }

    [Fact]
    public void TryGetProperty_WithNullDictionary_ReturnsDefault()
    {
        IReadOnlyDictionary<string, object>? dict = null;
        var result = dict.TryGetProperty<string>("key");
        Assert.Null(result);
    }
}

