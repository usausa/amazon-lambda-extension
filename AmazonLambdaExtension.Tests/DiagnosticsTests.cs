namespace AmazonLambdaExtension;

using Xunit;

// LambdaGenerator の診断（ALE0001〜ALE0024）の発生条件を、対象 ID 順に検証するテスト。
public sealed class DiagnosticsTests
{
    private static List<string> GetDiagnosticIds(string source)
        => CompilationHelper.RunGenerator(source).Diagnostics.Select(d => d.Id).ToList();

    // ALE0001
    [Fact]
    public void WhenNotPartial_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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
    public void WhenPartial_NoDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0002
    [Fact]
    public void WhenLambdaClassIsGeneric_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0002 / ALE0003 / ALE0004
    [Fact]
    public void WhenTopLevelNonGeneric_NoDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0003
    [Fact]
    public void WhenLambdaClassIsNested_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0004
    [Fact]
    public void WhenLambdaClassIsRecord_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0005
    [Fact]
    public void WhenLambdaClassIsAbstract_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0006
    [Fact]
    public void WhenServiceResolverMissingConfigureServices_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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
    public void WhenInternalResolverWithAccessibleConfigureServices_NoDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0007
    [Fact]
    public void WhenCtorParamsButNoServiceResolver_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0008
    [Fact]
    public void WhenNoParameterlessCtorWithoutServiceResolver_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0008 / ALE0011
    [Fact]
    public void WhenParameterlessCtorsWithoutServiceResolver_NoDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0009
    [Fact]
    public void WhenFilterTypeNotImplementILambdaFilter_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0010
    [Fact]
    public void WhenAbstractFilterWithoutServiceResolver_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0011
    [Fact]
    public void WhenFilterHasNoPublicParameterlessCtorWithoutServiceResolver_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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
    public void WhenInternalFilterWithAccessibleCtor_NoDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0013
    [Fact]
    public void WhenMultipleHandlerAttributes_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0014
    [Fact]
    public void WhenAuthorizerMethodMissing_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0015
    [Fact]
    public void WhenMultipleBindingAttributes_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0016
    [Fact]
    public void WhenFromBodyOnEventHandler_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0017
    [Fact]
    public void WhenInvalidBindingOnEventHandler_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0018
    [Fact]
    public void WhenFromAuthorizerOutsideHttpApi_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0019
    [Fact]
    public void WhenUnsupportedBindingType_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0020
    [Fact]
    public void WhenEventHandlerHasNoPayload_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0021
    [Fact]
    public void WhenEventHandlerHasMultiplePayloads_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0020 / ALE0021
    [Fact]
    public void WhenEventHandlerHasSinglePayload_NoDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0022
    [Fact]
    public void WhenAuthorizerInvalidReturnType_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0023
    [Fact]
    public void WhenFromServicesWithoutServiceResolver_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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

    // ALE0024
    [Fact]
    public void WhenHandlerIsOverloaded_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
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
