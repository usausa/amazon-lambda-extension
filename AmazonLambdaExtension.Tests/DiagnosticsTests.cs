namespace AmazonLambdaExtension;

using Xunit;

// LambdaGenerator の診断（ALE0001〜ALE0024）の発生条件を検証するテスト。
public sealed class DiagnosticsTests
{
    private static List<string> GetDiagnosticIds(string source)
    {
        var diagnostics = CompilationHelper.RunGeneratorWithDiagnostics(source);
        return diagnostics.Select(d => d.Id).ToList();
    }

    // ALE0001: [Lambda] クラスが partial でない
    [Fact]
    public void ALE0001_WhenNotPartial_ReportsDiagnostic()
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
    public void ALE0001_WhenPartial_NoDiagnostic()
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

    // ALE0013: 同一メソッドに複数のハンドラ属性
    [Fact]
    public void ALE0013_WhenMultipleHandlerAttributes_ReportsDiagnostic()
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

    [Fact]
    public void ALE0015_WhenMultipleBindingAttributes_ReportsDiagnostic()
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

    // ALE0016: [Event] ハンドラへの [FromBody]
    [Fact]
    public void ALE0016_WhenFromBodyOnEventHandler_ReportsDiagnostic()
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

    // ALE0022: [HttpApiAuthorizer] の戻り値型が IAuthorizerResult でない
    [Fact]
    public void ALE0022_WhenAuthorizerInvalidReturnType_ReportsDiagnostic()
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

    [Fact]
    public void ALE0014_WhenAuthorizerMethodMissing_ReportsDiagnostic()
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

    [Fact]
    public void ALE0018_WhenFromCustomAuthorizerOutsideHttpApi_ReportsDiagnostic()
    {
        var ids = GetDiagnosticIds("""
            namespace Test;
            using AmazonLambdaExtension.Annotations;
            [Lambda]
            public sealed partial class Function
            {
                [FunctionUrl]
                public string Handle([FromCustomAuthorizer("role")] string role) => role;
            }
            """);
        Assert.Contains("ALE0018", ids);
    }

    [Fact]
    public void ALE0019_WhenUnsupportedBindingType_ReportsDiagnostic()
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

    // ALE0007: コンストラクタ引数ありだが [ServiceResolver] なし
    [Fact]
    public void ALE0007_WhenCtorParamsButNoServiceResolver_ReportsDiagnostic()
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

    // ALE0006: ServiceResolver に ConfigureServices() がない
    [Fact]
    public void ALE0006_WhenServiceResolverMissingConfigureServices_ReportsDiagnostic()
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

    // ALE0009: [Filter<T>] で T が ILambdaFilter を実装していない
    [Fact]
    public void ALE0009_WhenFilterTypeNotImplementILambdaFilter_ReportsDiagnostic()
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

    [Fact]
    public void ALE0017_WhenInvalidBindingOnEventHandler_ReportsDiagnostic()
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

    [Fact]
    public void ALE0023_WhenFromServicesWithoutServiceResolver_ReportsDiagnostic()
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

    // ALE0020: [Event] ハンドラに payload 引数が無い
    [Fact]
    public void ALE0020_WhenEventHandlerHasNoPayload_ReportsDiagnostic()
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

    // ALE0021: [Event] ハンドラに payload 引数が複数ある
    [Fact]
    public void ALE0021_WhenEventHandlerHasMultiplePayloads_ReportsDiagnostic()
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

    // payload がちょうど 1 件なら ALE0020 / ALE0021 は発生しない
    [Fact]
    public void ALE0020_0021_WhenEventHandlerHasSinglePayload_NoDiagnostic()
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

    // ALE0024: 同名ハンドラーのオーバーロード
    [Fact]
    public void ALE0024_WhenHandlerIsOverloaded_ReportsDiagnostic()
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

    // ALE0002: [Lambda] クラスがジェネリック
    [Fact]
    public void ALE0002_WhenLambdaClassIsGeneric_ReportsDiagnostic()
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

    // ALE0003: [Lambda] クラスがネストされた型
    [Fact]
    public void ALE0003_WhenLambdaClassIsNested_ReportsDiagnostic()
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

    // トップレベルの非ジェネリッククラスでは ALE0002 / ALE0003 / ALE0004 は発生しない
    [Fact]
    public void ALE0002_0003_0004_WhenTopLevelNonGeneric_NoDiagnostic()
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

    // ALE0004: [Lambda] が record（record class）
    [Fact]
    public void ALE0004_WhenLambdaClassIsRecord_ReportsDiagnostic()
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

    // ALE0008: [ServiceResolver] 無しで Lambda クラスに parameterless ctor が無い
    [Fact]
    public void ALE0008_WhenNoParameterlessCtorWithoutServiceResolver_ReportsDiagnostic()
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

    // ALE0011: [ServiceResolver] 無しで Filter に public parameterless ctor が無い
    [Fact]
    public void ALE0011_WhenFilterHasNoPublicParameterlessCtorWithoutServiceResolver_ReportsDiagnostic()
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

    // [ServiceResolver] 無しでも parameterless ctor があれば ALE0008 / ALE0011 は出ない
    [Fact]
    public void ALE0008_0011_WhenParameterlessCtorsWithoutServiceResolver_NoDiagnostic()
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

    // ALE0011: 同一アセンブリの internal filter は Lambda クラスから到達可能なので診断されない
    [Fact]
    public void ALE0011_WhenInternalFilterWithAccessibleCtor_NoDiagnostic()
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

    // ALE0006: 同一アセンブリの internal resolver + internal static ConfigureServices() は到達可能なので診断されない
    [Fact]
    public void ALE0006_WhenInternalResolverWithAccessibleConfigureServices_NoDiagnostic()
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

    // ALE0005: abstract な [Lambda] クラス
    [Fact]
    public void ALE0005_WhenLambdaClassIsAbstract_ReportsDiagnostic()
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

    // ALE0010: [ServiceResolver] 無しで abstract な filter
    [Fact]
    public void ALE0010_WhenAbstractFilterWithoutServiceResolver_ReportsDiagnostic()
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
}
