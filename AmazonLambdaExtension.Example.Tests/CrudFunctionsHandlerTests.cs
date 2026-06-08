namespace AmazonLambdaExtension.Example.Tests;

using System.Text.Json;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using AmazonLambdaExtension.Example;

using Xunit;

public class CrudFunctionsHandlerTests
{
    private static APIGatewayHttpApiV2ProxyRequest MakeRequest(
        string method = "GET",
        string? body = null,
        Dictionary<string, string>? headers = null,
        Dictionary<string, string>? query = null,
        Dictionary<string, string>? path = null)
    {
        return new APIGatewayHttpApiV2ProxyRequest
        {
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
            {
                Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription
                {
                    Method = method
                }
            },
            Body = body,
            Headers = headers ?? [],
            QueryStringParameters = query ?? [],
            PathParameters = path ?? []
        };
    }

    [Fact]
    public async Task GetItem_Handler_ExistingId_Returns200()
    {
        var req = MakeRequest(
            path: new Dictionary<string, string> { ["id"] = "item-1" },
            query: new Dictionary<string, string> { ["page"] = "1" });
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.GetItem_Handler(req, ctx);

        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_Handler_MissingId_Returns404()
    {
        var req = MakeRequest(
            path: new Dictionary<string, string> { ["id"] = "not-exist" },
            query: new Dictionary<string, string> { ["page"] = "0" });
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.GetItem_Handler(req, ctx);

        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_Handler_InvalidPage_Returns400()
    {
        var req = MakeRequest(
            path: new Dictionary<string, string> { ["id"] = "item-1" },
            query: new Dictionary<string, string> { ["page"] = "notanumber" });
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.GetItem_Handler(req, ctx);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task ListItems_Handler_ValidRequest_Returns200()
    {
        var req = MakeRequest(
            query: new Dictionary<string, string> { ["ids"] = "1,2" },
            headers: new Dictionary<string, string> { ["x-tenant-id"] = "tenant-a" });
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.ListItems_Handler(req, ctx);

        Assert.Equal(200, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_Handler_AdminRole_Returns201()
    {
        var body = JsonSerializer.Serialize(new { name = "Widget", description = "A test widget" });
        var req = MakeRequest(
            method: "POST",
            body: body,
            headers: new Dictionary<string, string> { ["content-type"] = "application/json" });
        req.RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "POST" },
            Authorizer = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
            {
                Lambda = new Dictionary<string, object> { ["role"] = "admin" }
            }
        };
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.CreateItem_Handler(req, ctx);

        Assert.Equal(201, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_Handler_NonAdminRole_Returns403()
    {
        var body = JsonSerializer.Serialize(new { name = "Widget", description = "A test widget" });
        var req = MakeRequest(method: "POST", body: body);
        req.RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "POST" },
            Authorizer = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
            {
                Lambda = new Dictionary<string, object> { ["role"] = "viewer" }
            }
        };
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.CreateItem_Handler(req, ctx);

        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_Handler_ValidationFails_EmptyName_Returns400()
    {
        var body = JsonSerializer.Serialize(new { name = string.Empty });
        var req = MakeRequest(method: "POST", body: body);
        req.RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "POST" },
            Authorizer = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
            {
                Lambda = new Dictionary<string, object> { ["role"] = "admin" }
            }
        };
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.CreateItem_Handler(req, ctx);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_Handler_ValidationFails_NameTooLong_Returns400()
    {
        var body = JsonSerializer.Serialize(new { name = new string('x', 101) });
        var req = MakeRequest(method: "POST", body: body);
        req.RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext
        {
            Http = new APIGatewayHttpApiV2ProxyRequest.HttpDescription { Method = "POST" },
            Authorizer = new APIGatewayHttpApiV2ProxyRequest.AuthorizerDescription
            {
                Lambda = new Dictionary<string, object> { ["role"] = "admin" }
            }
        };
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.CreateItem_Handler(req, ctx);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task Authorize_Handler_ValidToken_ReturnsIsAuthorizedTrue()
    {
        var req = MakeRequest(headers: new Dictionary<string, string> { ["authorization"] = "valid-token" });
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.Authorize_Handler(req, ctx);

        Assert.True(response.IsAuthorized);
    }

    [Fact]
    public async Task Authorize_Handler_InvalidToken_ReturnsIsAuthorizedFalse()
    {
        var req = MakeRequest(headers: new Dictionary<string, string> { ["authorization"] = "bad-token" });
        var ctx = new TestLambdaContext();

        var response = await CrudFunctions.Authorize_Handler(req, ctx);

        Assert.False(response.IsAuthorized);
    }
}
