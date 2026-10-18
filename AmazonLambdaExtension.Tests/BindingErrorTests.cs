namespace AmazonLambdaExtension;

using Amazon.Lambda.APIGatewayEvents;

public sealed class BindingErrorTests
{
    private const string Head =
        """
        namespace Test;

        using System.Collections.Generic;
        using System.ComponentModel.DataAnnotations;
        using System.Threading.Tasks;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;
        using AmazonLambdaExtension.Binders;
        using AmazonLambdaExtension.Filters;
        using AmazonLambdaExtension.Serialization;
        using AmazonLambdaExtension.Validation;
        using Microsoft.Extensions.DependencyInjection;

        public sealed class Item
        {
            public string? Name { get; set; }
        }

        public sealed class PassFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
        }

        public sealed class FailingValidator : IRequestValidator
        {
            public bool Validate(object value) => false;

            public bool Validate(object value, ICollection<ValidationResult> results)
            {
                results.Add(new ValidationResult("Name is required.", ["/name"]));
                return false;
            }
        }

        public sealed class TestErrorFactory : IBindingErrorResultFactory
        {
            public IHttpResult Create(BindingError bindingError) =>
                HttpResults.NewResult((System.Net.HttpStatusCode)422, $"{bindingError.Kind}:{bindingError.Source}:{bindingError.Name}:{bindingError.ValidationResults.Count}");
        }

        """;

    private static string Resolver(bool registerFactory) =>
        $$"""
        public static class Resolver
        {
            public static IServiceCollection ConfigureServices()
            {
                var services = new ServiceCollection();
                services.AddSingleton<Amazon.Lambda.Core.ILambdaSerializer>(new Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer());
                services.AddSingleton<IBodySerializer>(JsonBodySerializer.Default);
                services.AddSingleton<IRequestValidator>(new FailingValidator());
                services.AddSingleton<PassFilter>();
                {{(registerFactory ? "services.AddSingleton<IBindingErrorResultFactory>(new TestErrorFactory());" : string.Empty)}}
                return services;
            }
        }

        """;

    private static string Function(string filter) =>
        $$"""
        [Lambda]
        [ServiceResolver(typeof(Resolver))]
        {{filter}}
        public sealed partial class Function
        {
            [HttpApi(LambdaHttpMethod.Get, "/query")]
            public IHttpResult Query([FromQuery] int page) => HttpResults.Ok();

            [HttpApi(LambdaHttpMethod.Get, "/route/{id}")]
            public IHttpResult Route([FromRoute] int id) => HttpResults.Ok();

            [HttpApi(LambdaHttpMethod.Get, "/header")]
            public IHttpResult Header([FromHeader("x-count")] int count) => HttpResults.Ok();

            [HttpApi(LambdaHttpMethod.Post, "/body")]
            public IHttpResult Body([FromBody] Item item) => HttpResults.Ok();

            [HttpApi(LambdaHttpMethod.Get, "/poco")]
            public string Poco([FromQuery] int page) => "ok";
        }
        """;

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task InvalidValueIsPassedToFactory(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + Resolver(registerFactory: true) + Function(filter));

        await AssertFactoryResult(assembly, "Query_Handler", new APIGatewayHttpApiV2ProxyRequest { QueryStringParameters = new Dictionary<string, string> { ["page"] = "x" } }, "InvalidValue:Query:page:0");
        await AssertFactoryResult(assembly, "Route_Handler", new APIGatewayHttpApiV2ProxyRequest { PathParameters = new Dictionary<string, string> { ["id"] = "x" } }, "InvalidValue:Route:id:0");
        await AssertFactoryResult(assembly, "Header_Handler", new APIGatewayHttpApiV2ProxyRequest { Headers = new Dictionary<string, string> { ["x-count"] = "x" } }, "InvalidValue:Header:x-count:0");
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task BodyErrorsArePassedToFactory(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + Resolver(registerFactory: true) + Function(filter));

        await AssertFactoryResult(assembly, "Body_Handler", new APIGatewayHttpApiV2ProxyRequest { Body = "{" }, "InvalidBody:Body:item:0");
        await AssertFactoryResult(assembly, "Body_Handler", new APIGatewayHttpApiV2ProxyRequest { Body = "null" }, "MissingBody:Body:item:0");
        await AssertFactoryResult(assembly, "Body_Handler", new APIGatewayHttpApiV2ProxyRequest { Body = "{}" }, "ValidationFailed:Body:item:1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public Task PocoHandlerUsesFactory(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + Resolver(registerFactory: true) + Function(filter));

        return AssertFactoryResult(assembly, "Poco_Handler", new APIGatewayHttpApiV2ProxyRequest { QueryStringParameters = new Dictionary<string, string> { ["page"] = "x" } }, "InvalidValue:Query:page:0");
    }

    //--------------------------------------------------------------------------------
    // Without factory
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task WithoutFactoryReturnsTextBadRequest(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + Resolver(registerFactory: false) + Function(filter));

        var query = await CompilationHelper.InvokeAsync(assembly, "Query_Handler", new APIGatewayHttpApiV2ProxyRequest { QueryStringParameters = new Dictionary<string, string> { ["page"] = "x" } });
        Assert.Equal(400, query.StatusCode);
        Assert.Equal("Invalid parameter: page", query.Body);

        var poco = await CompilationHelper.InvokeAsync(assembly, "Poco_Handler", new APIGatewayHttpApiV2ProxyRequest { QueryStringParameters = new Dictionary<string, string> { ["page"] = "x" } });
        Assert.Equal(400, poco.StatusCode);
        Assert.Equal("Invalid parameter: page", poco.Body);

        var body = await CompilationHelper.InvokeAsync(assembly, "Body_Handler", new APIGatewayHttpApiV2ProxyRequest { Body = "{}" });
        Assert.Equal(400, body.StatusCode);
        Assert.StartsWith("Validation failed: item", body.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutServiceResolverFactoryIsNotGenerated()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/query")]
                public IHttpResult Query([FromQuery] int page) => HttpResults.Ok();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);

        Assert.DoesNotContain(result.Sources.Values, static s => s.Contains("__bindingErrorResultFactory__", StringComparison.Ordinal));
    }

    private static async Task AssertFactoryResult(System.Reflection.Assembly assembly, string handler, APIGatewayHttpApiV2ProxyRequest request, string expected)
    {
        var response = await CompilationHelper.InvokeAsync(assembly, handler, request).ConfigureAwait(false);

        Assert.Equal(422, response.StatusCode);
        Assert.Equal(expected, response.Body);
    }
}
