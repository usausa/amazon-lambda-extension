namespace AmazonLambdaExtension;

using System.Reflection;

using AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;

public class DiagnosticTests
{
    private static List<string> GetDiagnosticIds(string source)
        => CompilationHelper.RunGenerator(source).Diagnostics.Select(d => d.Id).ToList();

    private static List<string> GetEntryPointDiagnosticIds(string source)
        => CompilationHelper.RunEntryPointGenerator(source).Diagnostics.Select(d => d.Id).ToList();

    private static List<string> GetEntryPointDiagnosticIds(string source, OutputKind outputKind, bool publishAot)
        => CompilationHelper.RunEntryPointGenerator(source, outputKind, publishAot).Diagnostics.Select(d => d.Id).ToList();

    // ------------------------------------------------------------
    // Class structure (00xx)
    // ------------------------------------------------------------

    [Fact]
    public void Ale0001NotPartialEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed class Function
            {
                [Event]
                public void Handle() { }
            }
            """);
        Assert.Contains("ALE0001", ids);
    }

    [Fact]
    public void Ale0001PartialEmitsNoDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle() { }
            }
            """);
        Assert.DoesNotContain("ALE0001", ids);
    }

    [Fact]
    public void Ale0002LambdaClassIsGenericEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function<T>
            {
                [Event]
                public void Handle() { }
            }
            """);
        Assert.Contains("ALE0002", ids);
    }

    [Fact]
    public void Ale0002TopLevelNonGenericEmitsNoDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public sealed class MyEvent { }
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle(MyEvent ev) { }
            }
            """);
        Assert.DoesNotContain("ALE0002", ids);
        Assert.DoesNotContain("ALE0003", ids);
        Assert.DoesNotContain("ALE0004", ids);
    }

    [Fact]
    public void Ale0003LambdaClassIsNestedEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public partial class Outer
            {
                [Lambda]
                public sealed partial class Inner
                {
                    [Event]
                    public void Handle() { }
                }
            }
            """);
        Assert.Contains("ALE0003", ids);
    }

    [Fact]
    public void Ale0004LambdaClassIsRecordEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public sealed class MyEvent { }
            [Lambda]
            public partial record Function
            {
                [Event]
                public void Handle(MyEvent ev) { }
            }
            """);
        Assert.Contains("ALE0004", ids);
    }

    [Fact]
    public void Ale0005LambdaClassIsAbstractEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public sealed class MyEvent { }
            [Lambda]
            public abstract partial class Function
            {
                [Event]
                public void Handle(MyEvent ev) { }
            }
            """);
        Assert.Contains("ALE0005", ids);
    }

    // ------------------------------------------------------------
    // Construction (01xx)
    // ------------------------------------------------------------

    [Fact]
    public void Ale0101CtorParamsButNoServiceResolverEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public interface IService { }
            [Lambda]
            public sealed partial class Function
            {
                private readonly IService svc;
                public Function(IService svc) { this.svc = svc; }
                [Event]
                public void Handle() { }
            }
            """);
        Assert.Contains("ALE0101", ids);
    }

    [Fact]
    public void Ale0102ServiceResolverMissingConfigureServicesEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            [ServiceResolver(typeof(BadResolver))]
            public sealed partial class Function
            {
                [Event]
                public void Handle() { }
            }
            public sealed class BadResolver { }
            """);
        Assert.Contains("ALE0102", ids);
    }

    [Fact]
    public void Ale0102InternalResolverWithAccessibleConfigureServicesEmitsNoDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using Microsoft.Extensions.DependencyInjection;
            public sealed class MyEvent { }
            [Lambda]
            [ServiceResolver(typeof(InternalResolver))]
            public sealed partial class Function
            {
                [Event]
                public void Handle(MyEvent ev) { }
            }
            internal sealed class InternalResolver
            {
                internal static IServiceCollection ConfigureServices() => new ServiceCollection();
            }
            """);
        Assert.DoesNotContain("ALE0102", ids);
    }

    [Fact]
    public void Ale0103NoParameterlessCtorWithoutServiceResolverEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public sealed class MyEvent { }
            [Lambda]
            public sealed partial class Function
            {
                private Function(int x) { }
                [Event]
                public void Handle(MyEvent ev) { }
            }
            """);
        Assert.Contains("ALE0103", ids);
    }

    [Fact]
    public void Ale0103ParameterlessCtorsWithoutServiceResolverEmitsNoDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;
            public sealed class MyFilter : ILambdaFilter
            {
                public ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next) => next(ctx);
            }
            [Lambda]
            [Filter<MyFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle() => HttpResults.Ok();
            }
            """);
        Assert.DoesNotContain("ALE0103", ids);
        Assert.DoesNotContain("ALE0203", ids);
    }

    [Fact]
    public void Ale0105FromServicesWithoutServiceResolverEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public interface IService { }
            public sealed class MyEvent { }
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle(MyEvent ev, [FromServices] IService service) { }
            }
            """);
        Assert.Contains("ALE0105", ids);
    }

    // ------------------------------------------------------------
    // Filter (02xx)
    // ------------------------------------------------------------

    [Fact]
    public void Ale0201FilterTypeNotImplementILambdaFilterEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            public sealed class NotAFilter { }
            [Lambda]
            [Filter<NotAFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle() => HttpResults.Ok(new { });
            }
            """);
        Assert.Contains("ALE0201", ids);
    }

    [Fact]
    public void Ale0202AbstractFilterWithoutServiceResolverEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;
            public abstract class AbstractFilter : ILambdaFilter
            {
                public abstract ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next);
            }
            [Lambda]
            [Filter<AbstractFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle() => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0202", ids);
    }

    [Fact]
    public void Ale0203FilterHasNoPublicParameterlessCtorWithoutServiceResolverEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;
            public sealed class MyFilter : ILambdaFilter
            {
                public MyFilter(int x) { }
                public ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next) => next(ctx);
            }
            [Lambda]
            [Filter<MyFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle() => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0203", ids);
    }

    [Fact]
    public void Ale0203InternalFilterWithAccessibleCtorEmitsNoDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;
            internal sealed class InternalFilter : ILambdaFilter
            {
                public ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next) => next(ctx);
            }
            [Lambda]
            [Filter<InternalFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle() => HttpResults.Ok();
            }
            """);
        Assert.DoesNotContain("ALE0203", ids);
    }

    // ------------------------------------------------------------
    // Handler (03xx)
    // ------------------------------------------------------------

    [Fact]
    public void Ale0301MultipleHandlerAttributesEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                [Event]
                public void Handle() { }
            }
            """);
        Assert.Contains("ALE0301", ids);
    }

    [Fact]
    public void Ale0302GenericHandlerEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public sealed class MyEvent { }
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle<T>(MyEvent ev) { }
            }
            """);
        Assert.Equal(["ALE0302"], ids);
    }

    [Fact]
    public void Ale0303EventHandlerHasNoPayloadEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using Amazon.Lambda.Core;
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle(ILambdaContext context) { }
            }
            """);
        Assert.Contains("ALE0303", ids);
    }

    [Fact]
    public void Ale0303EventHandlerHasSinglePayloadEmitsNoDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using Amazon.Lambda.Core;
            public sealed class MyEvent { }
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle(MyEvent ev, ILambdaContext context) { }
            }
            """);
        Assert.DoesNotContain("ALE0303", ids);
        Assert.DoesNotContain("ALE0304", ids);
    }

    [Fact]
    public void Ale0304EventHandlerHasMultiplePayloadsEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            public sealed class EventA { }
            public sealed class EventB { }
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle(EventA a, EventB b) { }
            }
            """);
        Assert.Contains("ALE0304", ids);
    }

    [Fact]
    public void Ale0305AuthorizerInvalidReturnTypeEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApiAuthorizer]
                public void Authorize() { }
            }
            """);
        Assert.Contains("ALE0305", ids);
    }

    [Fact]
    public void Ale0306HandlerIsOverloadedEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get(int id) => HttpResults.Ok();

                [HttpApi(LambdaHttpMethod.Get, "/b")]
                public IHttpResult Get(string id) => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0306", ids);
    }

    [Fact]
    public void Ale0307NoHandlerAttributeEmitsDiagnostic()
    {
        // Arrange
        const string source =
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

                public void Extra()
                {
                }
            }
            """;

        // Act
        var diagnostics = CompilationHelper.RunGenerator(source).Diagnostics;

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "ALE0307");
    }

    [Fact]
    public void Ale0308AuthorizerMethodMissingEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a", Authorizer = nameof(Authorize))]
                public IHttpResult Handle() => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0308", ids);
    }

    // ------------------------------------------------------------
    // Parameter (04xx)
    // ------------------------------------------------------------

    [Fact]
    public void Ale0401MultipleBindingAttributesEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle([FromQuery][FromRoute] string id) => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0401", ids);
    }

    [Fact]
    public void Ale0402FromBodyOnEventHandlerEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle([FromBody] string body) { }
            }
            """);
        Assert.Contains("ALE0402", ids);
    }

    [Theory]
    [InlineData("")]
    [InlineData("APIGatewayHttpApiV2ProxyRequest request, ")]
    public void Ale0402FromBodyOnAuthorizerEmitsDiagnostic(string request)
    {
        var source = $$"""
            namespace Test;
            using Amazon.Lambda.APIGatewayEvents;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApiAuthorizer]
                public IAuthorizerResult Authorize({{request}}[FromBody] string? body) => AuthorizerResults.Allow();
            }
            """;

        Assert.Equal(["ALE0402"], CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0403InvalidBindingOnEventHandlerEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void Handle([FromQuery] int value) { }
            }
            """);
        Assert.Contains("ALE0403", ids);
    }

    [Theory]
    [InlineData("[Event]", "public void Handle(SQSEvent ev, ClaimsPrincipal user) { }")]
    [InlineData("[HttpApiAuthorizer]", "public IAuthorizerResult Authorize(ClaimsPrincipal user) => AuthorizerResults.Allow();")]
    public void Ale0404ClaimsPrincipalOutsideHttpHandlerEmitsDiagnostic(string attribute, string method)
    {
        var ids = GetDiagnosticIds(
            $$"""
            namespace Test;
            using System.Security.Claims;
            using Amazon.Lambda.SQSEvents;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                {{attribute}}
                {{method}}
            }
            """);
        Assert.Equal(["ALE0404"], ids);
    }

    [Theory]
    [InlineData("[HttpApi(LambdaHttpMethod.Get, \"/a\")]")]
    [InlineData("[FunctionUrl]")]
    public void Ale0404ClaimsPrincipalOnHttpHandlerEmitsNoDiagnostic(string attribute)
    {
        var source =
            $$"""
            namespace Test;
            using System.Security.Claims;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                {{attribute}}
                public IHttpResult Get(ClaimsPrincipal user) => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0405FromCookieOnAuthorizerEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApiAuthorizer]
                public IAuthorizerResult Authorize([FromCookie("rt")] string? token) => AuthorizerResults.Allow();
            }
            """);
        Assert.Contains("ALE0405", ids);
    }

    [Fact]
    public void Ale0405FromCookieOnFunctionUrlEmitsNoDiagnostic()
    {
        var source =
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [FunctionUrl]
                public IHttpResult Ping([FromCookie("rt")] string? token) => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0406UnsupportedBindingTypeEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            public sealed class Input { public string? Name { get; set; } }
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Handle([FromQuery] Input input) => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0406", ids);
    }

    [Fact]
    public void Ale0406FromAuthorizerArrayEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get([FromAuthorizer("ids")] int[] ids) => HttpResults.Ok();
            }
            """);
        Assert.Equal(["ALE0406"], ids);
    }

    [Fact]
    public void Ale0406FromCookieArrayEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get([FromCookie("roles")] string[] roles) => HttpResults.Ok();
            }
            """);
        Assert.Contains("ALE0406", ids);
    }

    [Theory]
    [InlineData("/a", "[FromQuery] int[][] ids")]
    [InlineData("/a", "[FromQuery] int[,] ids")]
    [InlineData("/a", "[FromHeader(\"x-ids\")] int[][] ids")]
    [InlineData("/a", "[FromHeader(\"x-ids\")] int[,] ids")]
    [InlineData("/a/{ids}", "[FromRoute] int[][] ids")]
    [InlineData("/a/{ids}", "[FromRoute] int[,] ids")]
    public void Ale0406JaggedOrMultidimensionalArrayEmitsDiagnostic(string route, string parameter)
    {
        var source =
            $$"""
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "{{route}}")]
                public IHttpResult Get({{parameter}}) => HttpResults.Ok();
            }
            """;

        Assert.Equal(["ALE0406"], CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0407FromAuthorizerOutsideHttpApiEmitsDiagnostic()
    {
        var ids = GetDiagnosticIds(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function
            {
                [FunctionUrl]
                public string Handle([FromAuthorizer("role")] string role) => role;
            }
            """);
        Assert.Contains("ALE0407", ids);
    }

    [Theory]
    [InlineData("/items/{id}", "")]
    [InlineData("/items/{id}", "[FromRoute] int Id")]
    [InlineData("/items/{id}/{name}", "[FromRoute] int id")]
    [InlineData("/files/{proxy+}", "[FromQuery] string proxy")]
    public void Ale0410RouteVariableIsNotBoundEmitsDiagnostic(string route, string parameter)
    {
        var source =
            $$"""
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "{{route}}")]
                public IHttpResult Get({{parameter}}) => HttpResults.Ok();
            }
            """;

        Assert.Equal(["ALE0410"], CompilationHelper.GetProblemIds(source));
    }

    [Theory]
    [InlineData("/items/{id}", "[FromRoute] int id")]
    [InlineData("/items/{id}/{name}", "[FromRoute] int id, [FromRoute] string name")]
    [InlineData("/files/{proxy+}", "[FromRoute] string proxy")]
    [InlineData("/items", "")]
    public void Ale0410RouteVariableIsBoundEmitsNoDiagnostic(string route, string parameter)
    {
        var source =
            $$"""
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "{{route}}")]
                public IHttpResult Get({{parameter}}) => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0410RouteVariableBoundWithNameEmitsNoDiagnostic()
    {
        const string source =
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items/{id}")]
                public IHttpResult Get([FromRoute("id")] int itemId) => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0410CanBeSuppressedAtHandler()
    {
        const string source =
            """
            namespace Test;
            using Amazon.Lambda.APIGatewayEvents;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function
            {
            #pragma warning disable ALE0410
                [HttpApi(LambdaHttpMethod.Get, "/files/{proxy+}")]
                public IHttpResult Get(APIGatewayHttpApiV2ProxyRequest request) => HttpResults.Ok();
            #pragma warning restore ALE0410
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    // ------------------------------------------------------------
    // Entry point (06xx)
    // ------------------------------------------------------------

    [Fact]
    public void Ale0601MissingRuntimeSupportEmitsDiagnostic()
    {
        var ids = CompilationHelper.GetDiagnosticIdsWithoutRuntimeSupport(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
            namespace Test;
            public sealed class Placeholder { }
            """);
        Assert.Contains("ALE0601", ids);
    }

    [Fact]
    public void Ale0601RuntimeSupportReferencedEmitsNoDiagnostic()
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
            namespace Test;
            public sealed class Placeholder { }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public void Ale0602MissingLambdaSerializerEmitsDiagnostic()
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            namespace Test;
            public sealed class Placeholder { }
            """);
        Assert.Contains("ALE0602", ids);
    }

    [Fact]
    public void Ale0603SerializerWithoutParameterlessConstructorEmitsDiagnostic()
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Test.Serializer))]
            namespace Test;
            public sealed class Serializer : Amazon.Lambda.Core.ILambdaSerializer
            {
                public Serializer(int value) { }
                public T Deserialize<T>(System.IO.Stream requestStream) => throw new System.NotSupportedException();
                public void Serialize<T>(T response, System.IO.Stream responseStream) => throw new System.NotSupportedException();
            }
            """);
        Assert.Contains("ALE0603", ids);
    }

    [Fact]
    public void Ale0603SerializerNotImplementingInterfaceEmitsDiagnostic()
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Test.Serializer))]
            namespace Test;
            public sealed class Serializer { }
            """);
        Assert.Contains("ALE0603", ids);
    }

    [Fact]
    public void Ale0603AbstractSerializerEmitsDiagnostic()
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Test.Serializer))]
            namespace Test;
            public abstract class Serializer : Amazon.Lambda.Core.ILambdaSerializer
            {
                public T Deserialize<T>(System.IO.Stream requestStream) => throw new System.NotSupportedException();
                public void Serialize<T>(T response, System.IO.Stream responseStream) => throw new System.NotSupportedException();
            }
            """);
        Assert.Contains("ALE0603", ids);
    }

    [Fact]
    public void Ale0604TopLevelStatementsEmitDiagnostic()
    {
        var result = CompilationHelper.RunEntryPointGenerator(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
            System.Console.WriteLine("top-level");
            """,
            OutputKind.ConsoleApplication,
            publishAot: false);

        Assert.Contains("ALE0604", result.Diagnostics.Select(static d => d.Id));
        Assert.DoesNotContain(result.Sources.Values, static s => s.Contains("class LambdaEntryPoint", StringComparison.Ordinal));
    }

    [Fact]
    public void Ale0604TopLevelStatementsWithoutEntryPointEmitNoDiagnostic()
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            System.Console.WriteLine("top-level");
            """,
            OutputKind.ConsoleApplication,
            publishAot: false);
        Assert.Empty(ids);
    }

    [Theory]
    [InlineData(OutputKind.DynamicallyLinkedLibrary, true, true)]
    [InlineData(OutputKind.DynamicallyLinkedLibrary, false, false)]
    [InlineData(OutputKind.ConsoleApplication, true, false)]
    public void Ale0605PublishAotWithoutExecutableEmitsDiagnostic(OutputKind outputKind, bool publishAot, bool expected)
    {
        var ids = GetEntryPointDiagnosticIds(
            """
            [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
            [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
            namespace Test;
            public sealed class Placeholder { }
            """,
            outputKind,
            publishAot);
        Assert.Equal(expected, ids.Contains("ALE0605"));
    }

    // ------------------------------------------------------------
    // Valid
    // ------------------------------------------------------------

    [Fact]
    public void InterfaceImplementationAndOverrideAreNotHandlerCandidates()
    {
        const string source =
            """
            namespace Test;
            using System;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            [Lambda]
            public sealed partial class Function : IDisposable
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();

                public void Dispose()
                {
                }

                public override string ToString() => "Function";
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void ConstructorWithParametersBesideParameterlessConstructorIsAllowed()
    {
        const string source =
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            public interface IService { string Name { get; } }
            public sealed class Service : IService { public string Name => "real"; }
            [Lambda]
            public sealed partial class Function
            {
                private readonly IService service;

                public Function()
                    : this(new Service())
                {
                }

                public Function(IService service)
                {
                    this.service = service;
                }

                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok(service.Name);
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Theory]
    [InlineData("[HttpApi(LambdaHttpMethod.Get, \"/a\")]", "IHttpResult", "[FromQuery] string? keyword, [FromHeader(\"x-trace\")] string? trace")]
    [InlineData("[HttpApi(LambdaHttpMethod.Get, \"/a\")]", "IHttpResult", "APIGatewayHttpApiV2ProxyRequest? request, ILambdaContext? context")]
    [InlineData("[HttpApi(LambdaHttpMethod.Get, \"/a\")]", "IHttpResult?", "")]
    [InlineData("[HttpApiAuthorizer]", "IAuthorizerResult?", "APIGatewayCustomAuthorizerV2Request? request")]
    [InlineData("[Event]", "string?", "MyEvent? ev, ILambdaContext? context")]
    public void NullableAnnotationsAreAccepted(string attribute, string returnType, string parameters)
    {
        var source = $$"""
            namespace Test;
            using Amazon.Lambda.APIGatewayEvents;
            using Amazon.Lambda.Core;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            public sealed class MyEvent { }
            [Lambda]
            public sealed partial class Function
            {
                {{attribute}}
                public {{returnType}} Handle({{parameters}}) => default!;
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void NullableServiceCollectionIsAccepted()
    {
        const string source =
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using Microsoft.Extensions.DependencyInjection;
            public interface IService { }
            public sealed class Resolver
            {
                public static IServiceCollection? ConfigureServices() => new ServiceCollection();
            }
            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            public sealed partial class Function
            {
                internal Function(IService service)
                {
                    Service = service;
                }

                public IService Service { get; }

                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get([FromServices] IService? other, [FromServices("key")] IService? keyed) => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    // ------------------------------------------------------------
    // Reporting
    // ------------------------------------------------------------

    [Fact]
    public void DiagnosticIsReportedInSource()
    {
        var diagnostics = CompilationHelper.RunGenerator(
            """
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed class Function
            {
            }
            """).Diagnostics;

        var diagnostic = Assert.Single(diagnostics, static x => x.Id == "ALE0001");
        Assert.True(diagnostic.Location.IsInSource);
    }

    [Fact]
    public void ErrorsCannotBeSuppressed()
    {
        var descriptors = typeof(LambdaGenerator).Assembly.GetType("AmazonLambdaExtension.Generator.Diagnostics", throwOnError: true)!
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(static x => x.PropertyType == typeof(DiagnosticDescriptor))
            .Select(static x => (DiagnosticDescriptor)x.GetValue(null)!)
            .ToList();

        Assert.All(
            descriptors.Where(static x => x.DefaultSeverity == DiagnosticSeverity.Error),
            static x => Assert.Equal([WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.Compiler], x.CustomTags));
    }
}
