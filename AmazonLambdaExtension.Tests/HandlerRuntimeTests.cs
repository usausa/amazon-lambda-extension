namespace AmazonLambdaExtension;

using System.Text;
using System.Text.Json;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;

public sealed class HandlerRuntimeTests
{
    private const string Head =
        """
        namespace Test;

        using System.Threading.Tasks;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;
        using AmazonLambdaExtension.Filters;

        public sealed class PassFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
        }

        public sealed class DenyFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
            {
                context.Result = HttpResults.Unauthorized();
                return ValueTask.CompletedTask;
            }
        }

        """;

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task HandlerWithoutReturnValueReturnsOk(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + $$"""
            [Lambda]
            {{filter}}
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Delete, "/a")]
                public void Delete()
                {
                }

                [HttpApi(LambdaHttpMethod.Put, "/b")]
                public Task Put() => Task.CompletedTask;
            }
            """);

        Assert.Equal(200, (await CompilationHelper.InvokeAsync(assembly, "Delete_Handler", new APIGatewayHttpApiV2ProxyRequest())).StatusCode);
        Assert.Equal(200, (await CompilationHelper.InvokeAsync(assembly, "Put_Handler", new APIGatewayHttpApiV2ProxyRequest())).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task NullableHttpResultIsReturnedAsIs(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + $$"""
            [Lambda]
            {{filter}}
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public Task<IHttpResult?> Get() => Task.FromResult<IHttpResult?>(HttpResults.NotFound());

                [HttpApi(LambdaHttpMethod.Get, "/b")]
                public IHttpResult? Null() => null;
            }
            """);

        Assert.Equal(404, (await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest())).StatusCode);
        Assert.Equal(500, (await CompilationHelper.InvokeAsync(assembly, "Null_Handler", new APIGatewayHttpApiV2ProxyRequest())).StatusCode);
    }

    [Fact]
    public async Task FilterResultIsReturnedForPocoHandler()
    {
        var assembly = CompilationHelper.LoadAssembly(Head + """
            [Lambda]
            [Filter<DenyFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public string Get() => "ok";
            }
            """);

        Assert.Equal(401, (await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest())).StatusCode);
    }

    [Fact]
    public async Task BindingFailureWithFilterReturnsBadRequestForPocoHandler()
    {
        var assembly = CompilationHelper.LoadAssembly(Head + """
            [Lambda]
            [Filter<PassFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public string Get([FromQuery] int page) => page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            """);
        var request = new APIGatewayHttpApiV2ProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string> { ["page"] = "abc" }
        };

        Assert.Equal(400, (await CompilationHelper.InvokeAsync(assembly, "Get_Handler", request)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task ResponseBodyIsWrittenWithDefaultBodySerializer(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + $$"""
            public class Item
            {
                public string? ItemName { get; set; }
            }

            public sealed class DerivedItem : Item
            {
                public int Count { get; set; }
            }

            [Lambda]
            {{filter}}
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public Item Get() => new() { ItemName = "a" };

                [HttpApi(LambdaHttpMethod.Get, "/b")]
                public IHttpResult Derived() => HttpResults.Ok((Item)new DerivedItem { ItemName = "b", Count = 2 });
            }
            """);

        var response = await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest());
        var derived = await CompilationHelper.InvokeAsync(assembly, "Derived_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("""{"itemName":"a"}""", response.Body);
        Assert.Equal("application/json", response.Headers["content-type"]);
        Assert.Equal("""{"count":2,"itemName":"b"}""", derived.Body);
    }

    [Fact]
    public async Task RegisteredBodySerializerReadsRequestAndWritesResponse()
    {
        var assembly = CompilationHelper.LoadAssembly(Head + """
            public sealed class Item
            {
                public string? ItemName { get; set; }
            }

            public static class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                {
                    Microsoft.Extensions.DependencyInjection.IServiceCollection services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
                    services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(
                        typeof(AmazonLambdaExtension.Serialization.IBodySerializer),
                        new AmazonLambdaExtension.Serialization.JsonBodySerializer(new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower })));
                    return services;
                }
            }

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Post, "/a")]
                public Item Echo([FromBody(SkipValidate = true)] Item item) => new() { ItemName = item.ItemName + "!" };
            }
            """);

        var response = await CompilationHelper.InvokeAsync(assembly, "Echo_Handler", new APIGatewayHttpApiV2ProxyRequest { Body = """{"item_name":"a"}""" });

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("""{"item_name":"a!"}""", response.Body);
    }

    [Fact]
    public async Task AuthorizerContextOfRealEventIsBound()
    {
        const string json =
            """
            {
              "version": "2.0",
              "routeKey": "GET /a",
              "rawPath": "/a",
              "requestContext": {
                "http": { "method": "GET", "path": "/a" },
                "authorizer": { "lambda": { "role": "admin", "level": 3, "enabled": true } }
              }
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var request = new DefaultLambdaJsonSerializer().Deserialize<APIGatewayHttpApiV2ProxyRequest>(stream);
        var assembly = CompilationHelper.LoadAssembly(Head + """
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public string Get([FromAuthorizer("role")] string role, [FromAuthorizer("level")] int level, [FromAuthorizer("enabled")] bool enabled) =>
                    role + ":" + level.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + (enabled ? "yes" : "no");
            }
            """);

        var response = await CompilationHelper.InvokeAsync(assembly, "Get_Handler", request);

        Assert.IsType<JsonElement>(request.RequestContext.Authorizer.Lambda["role"]);
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("admin:3:yes", response.Body, StringComparison.Ordinal);
    }
}
