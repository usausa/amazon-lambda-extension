namespace AmazonLambdaExtension.Tests;

using System.Text.Json;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using AmazonLambdaExtension.Example;

using Xunit;

public class HealthCheckHandlerTests
{
    [Fact]
    public async Task Ping_Handler_Returns200WithStatus()
    {
        var req = new APIGatewayHttpApiV2ProxyRequest
        {
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "GET" },
            },
        };
        var ctx = new TestLambdaContext();

        var stream = await HealthCheck.Ping_Handler(req, ctx);

        stream.Position = 0;
        var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal(200, doc.RootElement.GetProperty("statusCode").GetInt32());
    }
}
