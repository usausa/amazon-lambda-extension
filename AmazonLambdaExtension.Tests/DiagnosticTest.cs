namespace AmazonLambdaExtension;

public class DiagnosticTest
{
    private static List<string> GetDiagnosticIds(string source)
        => CompilationHelper.RunGenerator(source).Diagnostics.Select(d => d.Id).ToList();

    // ------------------------------------------------------------
    // ALE0001
    // ------------------------------------------------------------

    [Fact]
    public void Ale0012NoHandlerAttributeEmitsDiagnostic()
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
        Assert.Contains(diagnostics, static x => x.Id == "ALE0012");
    }

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

    // ------------------------------------------------------------
    // ALE0002
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // ALE0002 / ALE0003 / ALE0004
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // ALE0003
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // ALE0004
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // ALE0005
    // ------------------------------------------------------------

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
    // ALE0006
    // ------------------------------------------------------------

    [Fact]
    public void Ale0006ServiceResolverMissingConfigureServicesEmitsDiagnostic()
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
        Assert.Contains("ALE0006", ids);
    }

    [Fact]
    public void Ale0006InternalResolverWithAccessibleConfigureServicesEmitsNoDiagnostic()
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
        Assert.DoesNotContain("ALE0006", ids);
    }

    // ------------------------------------------------------------
    // ALE0007
    // ------------------------------------------------------------

    [Fact]
    public void Ale0007CtorParamsButNoServiceResolverEmitsDiagnostic()
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
        Assert.Contains("ALE0007", ids);
    }

    // ------------------------------------------------------------
    // ALE0008
    // ------------------------------------------------------------

    [Fact]
    public void Ale0008NoParameterlessCtorWithoutServiceResolverEmitsDiagnostic()
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
        Assert.Contains("ALE0008", ids);
    }

    // ------------------------------------------------------------
    // ALE0008 / ALE0011
    // ------------------------------------------------------------

    [Fact]
    public void Ale0008ParameterlessCtorsWithoutServiceResolverEmitsNoDiagnostic()
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
        Assert.DoesNotContain("ALE0008", ids);
        Assert.DoesNotContain("ALE0011", ids);
    }

    // ------------------------------------------------------------
    // ALE0009
    // ------------------------------------------------------------

    [Fact]
    public void Ale0009FilterTypeNotImplementILambdaFilterEmitsDiagnostic()
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
        Assert.Contains("ALE0009", ids);
    }

    // ------------------------------------------------------------
    // ALE0010
    // ------------------------------------------------------------

    [Fact]
    public void Ale0010AbstractFilterWithoutServiceResolverEmitsDiagnostic()
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
        Assert.Contains("ALE0010", ids);
    }

    // ------------------------------------------------------------
    // ALE0011
    // ------------------------------------------------------------

    [Fact]
    public void Ale0011FilterHasNoPublicParameterlessCtorWithoutServiceResolverEmitsDiagnostic()
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
        Assert.Contains("ALE0011", ids);
    }

    [Fact]
    public void Ale0011InternalFilterWithAccessibleCtorEmitsNoDiagnostic()
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
        Assert.DoesNotContain("ALE0011", ids);
    }

    // ------------------------------------------------------------
    // ALE0013
    // ------------------------------------------------------------

    [Fact]
    public void Ale0013MultipleHandlerAttributesEmitsDiagnostic()
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
        Assert.Contains("ALE0013", ids);
    }

    // ------------------------------------------------------------
    // ALE0014
    // ------------------------------------------------------------

    [Fact]
    public void Ale0014AuthorizerMethodMissingEmitsDiagnostic()
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
        Assert.Contains("ALE0014", ids);
    }

    // ------------------------------------------------------------
    // ALE0015
    // ------------------------------------------------------------

    [Fact]
    public void Ale0015MultipleBindingAttributesEmitsDiagnostic()
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
        Assert.Contains("ALE0015", ids);
    }

    // ------------------------------------------------------------
    // ALE0016
    // ------------------------------------------------------------

    [Fact]
    public void Ale0016FromBodyOnEventHandlerEmitsDiagnostic()
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
        Assert.Contains("ALE0016", ids);
    }

    // ------------------------------------------------------------
    // ALE0017
    // ------------------------------------------------------------

    [Fact]
    public void Ale0017InvalidBindingOnEventHandlerEmitsDiagnostic()
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
        Assert.Contains("ALE0017", ids);
    }

    // ------------------------------------------------------------
    // ALE0018
    // ------------------------------------------------------------

    [Fact]
    public void Ale0018FromAuthorizerOutsideHttpApiEmitsDiagnostic()
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
        Assert.Contains("ALE0018", ids);
    }

    // ------------------------------------------------------------
    // ALE0019
    // ------------------------------------------------------------

    [Fact]
    public void Ale0019UnsupportedBindingTypeEmitsDiagnostic()
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
        Assert.Contains("ALE0019", ids);
    }

    // ------------------------------------------------------------
    // ALE0020
    // ------------------------------------------------------------

    [Fact]
    public void Ale0020EventHandlerHasNoPayloadEmitsDiagnostic()
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
        Assert.Contains("ALE0020", ids);
    }

    // ------------------------------------------------------------
    // ALE0020 / ALE0021
    // ------------------------------------------------------------

    [Fact]
    public void Ale0020EventHandlerHasSinglePayloadEmitsNoDiagnostic()
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
        Assert.DoesNotContain("ALE0020", ids);
        Assert.DoesNotContain("ALE0021", ids);
    }

    // ------------------------------------------------------------
    // ALE0021
    // ------------------------------------------------------------

    [Fact]
    public void Ale0021EventHandlerHasMultiplePayloadsEmitsDiagnostic()
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
        Assert.Contains("ALE0021", ids);
    }

    // ------------------------------------------------------------
    // ALE0022
    // ------------------------------------------------------------

    [Fact]
    public void Ale0022AuthorizerInvalidReturnTypeEmitsDiagnostic()
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
        Assert.Contains("ALE0022", ids);
    }

    // ------------------------------------------------------------
    // ALE0023
    // ------------------------------------------------------------

    [Fact]
    public void Ale0023FromServicesWithoutServiceResolverEmitsDiagnostic()
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
        Assert.Contains("ALE0023", ids);
    }

    // ------------------------------------------------------------
    // ALE0024
    // ------------------------------------------------------------

    [Fact]
    public void Ale0024HandlerIsOverloadedEmitsDiagnostic()
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
        Assert.Contains("ALE0024", ids);
    }
}
