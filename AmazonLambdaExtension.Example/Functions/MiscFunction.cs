namespace AmazonLambdaExtension.Example.Functions;

using AmazonLambdaExtension.Annotations;

using AmazonLambdaExtension.Example.Parameters;

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
[Filter(typeof(HttpApiFilter))]
public sealed class MiscFunction
{
    [HttpApi]
    public MiscTimeOutput Time()
    {
        return new MiscTimeOutput { DateTime = DateTime.Now };
    }

    [HttpApi]
    public int Calc(int x, int y)
    {
        return x + y;
    }

    [HttpApi]
    public async ValueTask<MiscHttpOutput> Http([FromServices] IHttpClientFactory httpClientFactory)
    {
        using var client = httpClientFactory.CreateClient(ConnectorNames.Ipify);

        var address = await client.GetStringAsync(string.Empty).ConfigureAwait(false);

        return new MiscHttpOutput { Address = address };
    }
}
