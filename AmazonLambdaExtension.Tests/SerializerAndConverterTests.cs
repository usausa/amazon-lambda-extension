namespace AmazonLambdaExtension;

using System.Globalization;
using System.Text.Json.Serialization;

using AmazonLambdaExtension.APIGateway;
using AmazonLambdaExtension.Binders;
using AmazonLambdaExtension.Serialization;

public sealed class SerializerAndConverterTests
{
    //--------------------------------------------------------------------------------
    // JsonBodySerializer.Default caching
    //--------------------------------------------------------------------------------

    [Fact]
    public void DefaultReturnsSameInstance()
    {
        var first = JsonBodySerializer.Default;
        var second = JsonBodySerializer.Default;

        Assert.Same(first, second);
    }

    //--------------------------------------------------------------------------------
    // HttpResult body written with IBodySerializer
    //--------------------------------------------------------------------------------

    [Fact]
    public void ObjectBodyIsWrittenByRuntimeTypeWithContext()
    {
        var serializer = new JsonBodySerializer(BodyJsonContext.Default);

        var item = ((IHttpResult)HttpResults.Ok(new BodyItem { ItemName = "a" })).ToResponse(serializer);
        var derived = ((IHttpResult)HttpResults.Ok(new DerivedBodyItem { ItemName = "b", Count = 2 })).ToResponse(serializer);
        var unregistered = ((IHttpResult)HttpResults.Ok(new UnregisteredBodyItem { ItemName = "c", Extra = "x" })).ToResponse(serializer);

        Assert.Equal("""{"itemName":"a"}""", item.Body);
        Assert.Equal("application/json", item.Headers["content-type"]);
        Assert.Equal("""{"count":2,"itemName":"b"}""", derived.Body);
        Assert.Equal("""{"itemName":"c"}""", unregistered.Body);
    }

    [Fact]
    public void AnonymousBodyIsNotSupportedWithContext()
    {
        var result = (IHttpResult)HttpResults.Ok(new { status = "ok" });

        Assert.Throws<NotSupportedException>(() => result.ToResponse(new JsonBodySerializer(BodyJsonContext.Default)));
    }

    //--------------------------------------------------------------------------------
    // StringConverter culture independence
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryToDoubleDotSeparatedValue()
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
    public void TryToDateTimeParsesIsoFormat()
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

public class BodyItem
{
    public string? ItemName { get; set; }
}

public sealed class DerivedBodyItem : BodyItem
{
    public int Count { get; set; }
}

public sealed class UnregisteredBodyItem : BodyItem
{
    public string? Extra { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BodyItem))]
[JsonSerializable(typeof(DerivedBodyItem))]
internal sealed partial class BodyJsonContext : JsonSerializerContext;
