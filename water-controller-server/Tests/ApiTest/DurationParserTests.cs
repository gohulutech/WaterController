using Api;
using Xunit;

namespace ApiTest;

public class DurationParserTests
{
    [Theory]
    [InlineData("30s", 30)]
    [InlineData("5m", 300)]
    [InlineData("1h", 3600)]
    [InlineData("24h", 86400)]
    [InlineData("7d", 604800)]
    [InlineData("1s", 1)]
    [InlineData("1m", 60)]
    public void TryParse_ValidInput_ReturnsCorrectSeconds(string input, long expected)
    {
        var result = DurationParser.TryParse(input, out var seconds);

        Assert.True(result);
        Assert.Equal(expected, seconds);
    }

    [Theory]
    [InlineData("30S", 30)]
    [InlineData("5M", 300)]
    [InlineData("1H", 3600)]
    [InlineData("7D", 604800)]
    public void TryParse_UpperCase_ReturnsCorrectSeconds(string input, long expected)
    {
        var result = DurationParser.TryParse(input, out var seconds);

        Assert.True(result);
        Assert.Equal(expected, seconds);
    }

    [Theory]
    [InlineData("  1h  ", 3600)]
    [InlineData(" 24h", 86400)]
    public void TryParse_WithWhitespace_ReturnsCorrectSeconds(string input, long expected)
    {
        var result = DurationParser.TryParse(input, out var seconds);

        Assert.True(result);
        Assert.Equal(expected, seconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_EmptyOrNull_ReturnsFalse(string? input)
    {
        var result = DurationParser.TryParse(input!, out var seconds);

        Assert.False(result);
        Assert.Equal(0, seconds);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1x")]
    [InlineData("hh")]
    [InlineData("1.5h")]
    [InlineData("-1h")]
    [InlineData("1")]
    [InlineData("h")]
    public void TryParse_InvalidFormat_ReturnsFalse(string input)
    {
        var result = DurationParser.TryParse(input, out var seconds);

        Assert.False(result);
        Assert.Equal(0, seconds);
    }

    [Fact]
    public void TryParse_LargeValue_ReturnsCorrectSeconds()
    {
        var result = DurationParser.TryParse("365d", out var seconds);

        Assert.True(result);
        Assert.Equal(31536000L, seconds);
    }

    [Fact]
    public void TryParse_Overflow_ReturnsFalse()
    {
        // Very large number that overflows long when multiplied
        var result = DurationParser.TryParse("9999999999999999999h", out var seconds);

        Assert.False(result);
        Assert.Equal(0, seconds);
    }
}
