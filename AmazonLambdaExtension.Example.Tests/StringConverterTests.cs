namespace AmazonLambdaExtension.Example.Tests;

using AmazonLambdaExtension.Binders;

using Xunit;

public class StringConverterTests
{
    // Boolean
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    public void TryToBoolean_ValidValues_ReturnsTrue(string input, bool expected)
    {
        var result = StringConverter.TryToBoolean(input.AsSpan(), out var value);
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Fact]
    public void TryToBoolean_InvalidValue_ReturnsFalse()
    {
        Assert.False(StringConverter.TryToBoolean("yes".AsSpan(), out _));
    }

    // Int32
    [Theory]
    [InlineData("0", 0)]
    [InlineData("42", 42)]
    [InlineData("-1", -1)]
    public void TryToInt32_ValidValues_ReturnsTrue(string input, int expected)
    {
        var result = StringConverter.TryToInt32(input.AsSpan(), out var value);
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Fact]
    public void TryToInt32_InvalidValue_ReturnsFalse()
    {
        Assert.False(StringConverter.TryToInt32("abc".AsSpan(), out _));
    }

    // Int64
    [Fact]
    public void TryToInt64_ValidValue_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToInt64("9999999999".AsSpan(), out var v));
        Assert.Equal(9999999999L, v);
    }

    // Double
    [Fact]
    public void TryToDouble_ValidValue_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToDouble("3.14".AsSpan(), out var v));
        Assert.Equal(3.14, v, precision: 10);
    }

    [Fact]
    public void TryToDouble_InvalidValue_ReturnsFalse()
    {
        Assert.False(StringConverter.TryToDouble("xyz".AsSpan(), out _));
    }

    // Guid
    [Fact]
    public void TryToGuid_ValidValue_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        Assert.True(StringConverter.TryToGuid(id.ToString().AsSpan(), out var v));
        Assert.Equal(id, v);
    }

    [Fact]
    public void TryToGuid_InvalidValue_ReturnsFalse()
    {
        Assert.False(StringConverter.TryToGuid("not-a-guid".AsSpan(), out _));
    }

    // DateTime
    [Fact]
    public void TryToDateTime_ValidIso_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToDateTime("2024-01-15".AsSpan(), out var v));
        Assert.Equal(2024, v.Year);
    }

    // Enum
    public enum Color
    {
        Red,
        Green,
        Blue
    }

    [Theory]
    [InlineData("Red", Color.Red)]
    [InlineData("green", Color.Green)]
    [InlineData("BLUE", Color.Blue)]
    public void TryToEnum_ValidValues_ReturnsTrue(string input, Color expected)
    {
        var result = StringConverter.TryToEnum<Color>(input.AsSpan(), out var value);
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Fact]
    public void TryToEnum_InvalidValue_ReturnsFalse()
    {
        Assert.False(StringConverter.TryToEnum<Color>("Yellow".AsSpan(), out _));
    }

    // Char
    [Fact]
    public void TryToChar_SingleChar_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToChar("A".AsSpan(), out var v));
        Assert.Equal('A', v);
    }

    [Fact]
    public void TryToChar_MultipleChars_ReturnsFalse()
    {
        Assert.False(StringConverter.TryToChar("AB".AsSpan(), out _));
    }

    // UInt32
    [Fact]
    public void TryToUInt32_ValidValue_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToUInt32("100".AsSpan(), out var v));
        Assert.Equal(100u, v);
    }

    // Decimal
    [Fact]
    public void TryToDecimal_ValidValue_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToDecimal("1.23".AsSpan(), out var v));
        Assert.Equal(1.23m, v);
    }

    // TimeSpan
    [Fact]
    public void TryToTimeSpan_ValidValue_ReturnsTrue()
    {
        Assert.True(StringConverter.TryToTimeSpan("01:30:00".AsSpan(), out var v));
        Assert.Equal(TimeSpan.FromMinutes(90), v);
    }
}
