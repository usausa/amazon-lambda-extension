namespace AmazonLambdaExtension.Example.Tests;

using System.Text.Json;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

public class HealthCheckHandlerTests
{
    [Fact]
    public async Task PingHandlerReturns200WithStatus()
    {
        var req = new APIGatewayHttpApiV2ProxyRequest
        {
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "GET" }
            }
        };
        var ctx = new TestLambdaContext();

        var response = await HealthCheckFunction.Ping_Handler(req, ctx);

        Assert.Equal(200, response.StatusCode);
        Assert.StartsWith("""{"status":"ok","timestamp":""", response.Body, StringComparison.Ordinal);

        var status = JsonSerializer.Deserialize<HealthStatus>(response.Body, JsonSerializerOptions.Web)!;
        Assert.Equal("ok", status.Status);
        Assert.NotEqual(default, status.Timestamp);
    }
}
