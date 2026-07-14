#pragma warning disable CA1707
namespace AmazonLambdaExtension;

using System.Globalization;

using AmazonLambdaExtension.Binders;
using AmazonLambdaExtension.Serialization;

public sealed class SerializerAndConverterTests
{
    //--------------------------------------------------------------------------------
    // JsonBodySerializer.Default caching
    //--------------------------------------------------------------------------------

    [Fact]
    public void Default_ReturnsSameInstance()
    {
        var first = JsonBodySerializer.Default;
        var second = JsonBodySerializer.Default;

        Assert.Same(first, second);
    }

    //--------------------------------------------------------------------------------
    // StringConverter culture independence
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryToDouble_WithDeDE_ParsesDotSeparatedValue()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var success = StringConverter.TryToDouble("1.5", out var result);

            Assert.True(success);
            Assert.Equal(1.5, result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void TryToDateTime_WithDeDE_ParsesIsoFormat()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var success = StringConverter.TryToDateTime("2026-06-10T01:02:03", out var result);

            Assert.True(success);
            Assert.Equal(new DateTime(2026, 6, 10, 1, 2, 3), result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
