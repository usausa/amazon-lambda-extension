namespace AmazonLambdaExtension.Example.Tests;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using AmazonLambdaExtension.Example;

using Xunit;

// フィルター付きハンドラ（LoggingFilter + ApiKeyFilter）の結合テスト。
// 生成ハンドラを実行し、フィルターパイプラインが実際に効くことを検証する。
public class SecureFunctionsHandlerTests
{
    private static APIGatewayHttpApiV2ProxyRequest MakeRequest(Dictionary<string, string>? headers = null)
    {
        return new APIGatewayHttpApiV2ProxyRequest
        {
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "GET" }
            },
            Headers = headers ?? [],
            PathParameters = new Dictionary<string, string> { ["id"] = "item-1" }
        };
    }

    [Fact]
    public async Task GetItem_Handler_ValidApiKey_Returns200()
    {
        var req = MakeRequest(new Dictionary<string, string> { ["x-api-key"] = "expected" });
        var ctx = new TestLambdaContext();

        var response = await SecureFunctions.GetItem_Handler(req, ctx);

        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_Handler_MissingApiKey_ShortCircuitsWith401()
    {
        var req = MakeRequest();
        var ctx = new TestLambdaContext();

        var response = await SecureFunctions.GetItem_Handler(req, ctx);

        Assert.Equal(401, response.StatusCode);
    }
}
