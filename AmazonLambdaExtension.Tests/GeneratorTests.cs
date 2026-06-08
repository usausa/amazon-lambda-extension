namespace AmazonLambdaExtension;

public sealed class GeneratorTests(ITestOutputHelper output)
{
    //--------------------------------------------------------------------------------
    // Service resolution
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenNoServiceResolver_ServiceResolverIsNull()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;

            public sealed class MyEvent { }

            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle(MyEvent ev)
                {
                }
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        Assert.NotEmpty(result.Sources);
    }

    [Fact]
    public void SharedFields_GeneratesStaticReadonlyProvider()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle()
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var sharedSource = result.Sources.Values.First(s => s.Contains("__provider__", StringComparison.Ordinal));
        output.WriteLine(sharedSource);

        Assert.Contains("static readonly", sharedSource, StringComparison.Ordinal);
        Assert.Contains("__provider__", sharedSource, StringComparison.Ordinal);
        Assert.Contains("__target__", sharedSource, StringComparison.Ordinal);
        Assert.Contains("BuildServiceProvider(", sharedSource, StringComparison.Ordinal);
        Assert.Contains("BuildProvider()", sharedSource, StringComparison.Ordinal);
        Assert.Contains("ValidateScopes", sharedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DiHandler_NoFilterNoFromServices_DoesNotCreateScope()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] int page)
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.DoesNotContain("CreateAsyncScope", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // Event handler
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenEventHandler_GeneratesHandlerMethod()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;

            public sealed class MyEvent { }

            [Lambda]
            public sealed partial class QueueFunction
            {
                [Event]
                public void Handle(MyEvent ev)
                {
                }
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        Assert.Single(result.Sources.Values, s => s.Contains("Handle_Handler", StringComparison.Ordinal));
    }

    [Fact]
    public void EventHandler_NoFilter_GeneratesCorrectStructure()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using Amazon.Lambda.SQSEvents;
            using Amazon.Lambda.Core;
            using AmazonLambdaExtension.Annotations;

            [Lambda]
            public sealed partial class QueueProcessor
            {
                [Event]
                public void Handle(SQSEvent ev, ILambdaContext context)
                {
                }
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("public static", handlerSource, StringComparison.Ordinal);
        Assert.Contains("Handle_Handler", handlerSource, StringComparison.Ordinal);
        Assert.Contains("global::Amazon.Lambda.SQSEvents.SQSEvent", handlerSource, StringComparison.Ordinal);
        Assert.Contains("global::Amazon.Lambda.Core.ILambdaContext", handlerSource, StringComparison.Ordinal);
        Assert.Contains("throw;", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void EventHandler_WithFromServices_NoFilter_GeneratesServiceBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using Amazon.Lambda.SQSEvents;

            public interface IProcessor
            {
                void Process(string value);
            }

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class QueueProcessor
            {
                [Event]
                public void Handle(SQSEvent ev, [FromServices] IProcessor processor)
                {
                    processor.Process(ev.Records[0].Body);
                }
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("CreateAsyncScope(__provider__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<global::Test.IProcessor>(__sp__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__target__.Handle(ev, p1!)", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // HttpApi / FunctionUrl handler
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenHttpApiHandler_GeneratesHandlerMethod()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class CrudFunctions
            {
                [HttpApi(LambdaHttpMethod.Get, "/items/{id}")]
                public IHttpResult GetItem()
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        Assert.Single(result.Sources.Values, s => s.Contains("GetItem_Handler", StringComparison.Ordinal));
    }

    [Fact]
    public void WhenFunctionUrlHandler_GeneratesHandlerMethod()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class HealthCheck
            {
                [FunctionUrl]
                public IHttpResult Ping()
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        Assert.Single(result.Sources.Values, s => s.Contains("Ping_Handler", StringComparison.Ordinal));
    }

    //--------------------------------------------------------------------------------
    // HttpApi result
    //--------------------------------------------------------------------------------

    [Fact]
    public void HttpApiHandler_IHttpResultReturn_NoFilter_GeneratesResponseReturn()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle()
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("global::Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse", handlerSource, StringComparison.Ordinal);
        Assert.Contains(".ToResponse(", handlerSource, StringComparison.Ordinal);
        Assert.Contains("global::AmazonLambdaExtension.ApiException", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_PocoReturn_NoFilter_WrapsWithHttpResultsOk()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public Item Handle()
                    => new Item();
            }

            public sealed class Item
            {
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("global::Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse", handlerSource, StringComparison.Ordinal);
        Assert.Contains("HttpResults.Ok(__result__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains(".ToResponse(", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_ProxyResponseReturn_NoFilter_ReturnsDirectly()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using Amazon.Lambda.APIGatewayEvents;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public APIGatewayHttpApiV2ProxyResponse Handle()
                    => new APIGatewayHttpApiV2ProxyResponse { StatusCode = 200 };
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("return __result__;", handlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".ToResponse(", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // Parameter binding
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenFromQuery_GeneratesQueryStringBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] string name)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("QueryStringParameters", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenFromRoute_GeneratesPathParameterBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items/{id}")]
                public IHttpResult GetItem([FromRoute] string id)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("GetItem_Handler", StringComparison.Ordinal));
        Assert.Contains("PathParameters", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenFromHeader_GeneratesHeaderBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromHeader("x-tenant-id")] string tenantId)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("Headers", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenFromQueryArray_GeneratesArrayBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] int[] ids)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("Split(',')", handlerSource, StringComparison.Ordinal);
        Assert.Contains("new int[", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenIntParameter_GeneratesStringConverterCall()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] int page)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("TryToInt32", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_FromRouteAndQuery_GeneratesCorrectBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items/{id}")]
                public IHttpResult GetItem([FromRoute] string id, [FromQuery] int page)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("GetItem_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("PathParameters", handlerSource, StringComparison.Ordinal);
        Assert.Contains(@"""id""", handlerSource, StringComparison.Ordinal);
        Assert.Contains("QueryStringParameters", handlerSource, StringComparison.Ordinal);
        Assert.Contains(@"""page""", handlerSource, StringComparison.Ordinal);
        Assert.Contains("TryToInt32", handlerSource, StringComparison.Ordinal);
        Assert.Contains("BadRequest", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_FromQueryArray_GeneratesArrayBinding()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] int[] ids)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("Split(',')", handlerSource, StringComparison.Ordinal);
        Assert.Contains("new int[", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // Default value binding
    //--------------------------------------------------------------------------------

    [Fact]
    public void HttpApiHandler_NullableQueryDefault_BindsDefaultValue()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] int? page = 1)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("(int?)1", handlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("(int?)null", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_EnumQueryDefault_BindsDefaultValue()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public enum Mode { Basic, Advanced }

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] Mode mode = Mode.Advanced)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("(global::Test.Mode)1", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_StringQueryDefault_BindsDefaultValue()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] string name = "guest")
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("(string)\"guest\"", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_NumericQueryDefault_UsesInvariantCulture()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromQuery] double rate = 1.5)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("(double)1.5", handlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("1,5", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // FromServices
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenFromServices_GeneratesServiceProviderResolution()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromServices] System.IDisposable service)
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("GetRequiredService", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenFromServicesWithKey_GeneratesKeyedServiceResolution()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle([FromServices("primary")] System.IDisposable service)
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("GetRequiredKeyedService", handlerSource, StringComparison.Ordinal);
        Assert.Contains("\"primary\"", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // FromBody
    //--------------------------------------------------------------------------------

    [Fact]
    public void FromBody_WithValidation_GeneratesRequestValidatorField()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public sealed class Input { public string? Name { get; set; } }

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Post, "/items")]
                public IHttpResult Create([FromBody] Input input)
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var sharedSource = result.Sources.Values.First(s => s.Contains("__provider__", StringComparison.Ordinal));
        output.WriteLine(sharedSource);

        Assert.Contains("__requestValidator__", sharedSource, StringComparison.Ordinal);
        Assert.Contains("global::AmazonLambdaExtension.Validation.IRequestValidator", sharedSource, StringComparison.Ordinal);

        var handlerSource = result.Sources.Values.Single(s => s.Contains("Create_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("__requestValidator__.Validate", handlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ValidationHelper", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void FromBody_SkipValidate_DoesNotGenerateRequestValidatorField()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public sealed class Input { public string? Name { get; set; } }

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Post, "/items")]
                public IHttpResult Create([FromBody(SkipValidate = true)] Input input)
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var sharedSource = result.Sources.Values.First(s => s.Contains("__provider__", StringComparison.Ordinal));
        output.WriteLine(sharedSource);

        Assert.DoesNotContain("__requestValidator__", sharedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void FromBody_WithoutServiceResolver_GeneratesDefaultSerializerAndValidator()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public sealed class Input { public string? Name { get; set; } }

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Post, "/items")]
                public IHttpResult Create([FromBody] Input input)
                    => HttpResults.Ok(input);
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var sharedSource = result.Sources.Values.First(s => s.Contains("__bodySerializer__", StringComparison.Ordinal));
        output.WriteLine(sharedSource);

        Assert.Contains("JsonBodySerializer.Default", sharedSource, StringComparison.Ordinal);
        Assert.Contains("new global::AmazonLambdaExtension.Validation.DataAnnotationsRequestValidator()", sharedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_FromBodyNonNullable_GeneratesRequiredAndInvalidChecks()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public sealed class Input { public string? Name { get; set; } }

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Post, "/items")]
                public IHttpResult Create([FromBody] Input input)
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Create_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("catch (global::System.Text.Json.JsonException)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("Invalid request body.", handlerSource, StringComparison.Ordinal);
        Assert.Contains("Request body is required.", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // Filter pipeline
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenFilterSpecified_GeneratesPipelineCode()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;

            public sealed class MyFilter : ILambdaFilter
            {
                public ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next)
                    => next(ctx);
            }

            [Lambda]
            [Filter<MyFilter>(Order = 0)]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class SecureFunctions
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle()
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.Contains("__pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenNoFilter_DoesNotGeneratePipelineCode()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class SimpleFunctions
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle()
                    => HttpResults.Ok(new { });
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.DoesNotContain("__pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpApiHandler_WithFilter_GeneratesPipelineStructure()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;

            public sealed class LoggingFilter : ILambdaFilter
            {
                public ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next)
                    => next(ctx);
            }

            [Lambda]
            [Filter<LoggingFilter>(Order = 0)]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class SecureFunctions
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult Handle()
                    => HttpResults.Ok(new { });
            }

            public sealed class Resolver
            {
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
                    => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("__pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("CreateAsyncScope(__provider__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__filter0__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<global::Test.LoggingFilter>(__sp__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("ctx.Result", handlerSource, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // HttpApiAuthorizer
    //--------------------------------------------------------------------------------

    [Fact]
    public void AuthorizerHandler_GeneratesCorrectStructure()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using System.Threading.Tasks;
            using Amazon.Lambda.APIGatewayEvents;
            using Amazon.Lambda.Core;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class AuthFunction
            {
                [HttpApiAuthorizer(EnableSimpleResponses = true)]
                public async ValueTask<IAuthorizerResult> Authorize(
                    APIGatewayHttpApiV2ProxyRequest request,
                    ILambdaContext context)
                {
                    return AuthorizerResults.Allow();
                }
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Authorize_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("Authorize_Handler", handlerSource, StringComparison.Ordinal);
        Assert.Contains("AuthorizerResult", handlerSource, StringComparison.Ordinal);
        Assert.Contains(".ToSimpleResponse(", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthorizerHandler_WithCustomAuthorizerRequest_UsesRouteArn()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using Amazon.Lambda.APIGatewayEvents;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class AuthFunction
            {
                [HttpApiAuthorizer(EnableSimpleResponses = false)]
                public IAuthorizerResult Authorize(APIGatewayCustomAuthorizerV2Request request)
                    => AuthorizerResults.Allow();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var handlerSource = result.Sources.Values.Single(s => s.Contains("Authorize_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("APIGatewayCustomAuthorizerV2Request", handlerSource, StringComparison.Ordinal);
        Assert.Contains(".ToIamResponse(request.RouteArn)", handlerSource, StringComparison.Ordinal);
    }
}
