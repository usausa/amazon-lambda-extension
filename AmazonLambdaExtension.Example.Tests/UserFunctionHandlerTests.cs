namespace AmazonLambdaExtension.Example.Tests;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

public class UserFunctionHandlerTests
{
    private static APIGatewayHttpApiV2ProxyRequest MakeRequest(Dictionary<string, string>? claims)
    {
        return new APIGatewayHttpApiV2ProxyRequest
        {
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "GET" },
                Authorizer = claims is null
                    ? null
                    : new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
                    {
                        Jwt = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription.JwtDescription { Claims = claims }
                    }
            },
            Cookies = ["theme=dark"]
        };
    }

    [Fact]
    public async Task MeHandlerAuthenticatedUserReturns200()
    {
        var req = MakeRequest(new Dictionary<string, string> { ["sub"] = "user-1", ["role"] = "admin" });
        var ctx = new TestLambdaContext();

        var response = await UserFunction.Me_Handler(req, ctx);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("""{"id":"user-1","admin":true,"theme":"dark"}""", response.Body);
    }

    [Fact]
    public async Task MeHandlerUnauthenticatedUserReturns401()
    {
        var req = MakeRequest(null);
        var ctx = new TestLambdaContext();

        var response = await UserFunction.Me_Handler(req, ctx);

        Assert.Equal(401, response.StatusCode);
    }
}
