namespace AmazonLambdaExtension.Example.Tests;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

public class HealthCheckHandlerTests
{
    [Fact]
    public async Task Ping_Handler_Returns200WithStatus()
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
    }
}
