namespace AmazonLambdaExtension;

using Xunit;

// LambdaGenerator の診断（ALE0001〜ALE0012）の発生条件を検証するテスト。
public sealed class DiagnosticsTests
{
    private static List<string> GetDiagnosticIds(string source)
    {
        var (_, diagnostics) = CompilationHelper.RunGeneratorWithDiagnostics(source);
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

    // ALE0003: 同一メソッドに複数のハンドラ属性
    [Fact]
    public void ALE0003_WhenMultipleHandlerAttributes_ReportsDiagnostic()
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
        Assert.Contains("ALE0003", ids);
    }

    // ALE0005: [Event] ハンドラへの [FromBody]
    [Fact]
    public void ALE0005_WhenFromBodyOnEventHandler_ReportsDiagnostic()
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
        Assert.Contains("ALE0005", ids);
    }

    // ALE0007: [HttpApiAuthorizer] の戻り値型が IAuthorizerResult でない
    [Fact]
    public void ALE0007_WhenAuthorizerInvalidReturnType_ReportsDiagnostic()
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
        Assert.Contains("ALE0007", ids);
    }

    // ALE0010: コンストラクタ引数ありだが [ServiceResolver] なし
    [Fact]
    public void ALE0010_WhenCtorParamsButNoServiceResolver_ReportsDiagnostic()
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
        Assert.Contains("ALE0010", ids);
    }

    // ALE0011: ServiceResolver に ConfigureServices() がない
    [Fact]
    public void ALE0011_WhenServiceResolverMissingConfigureServices_ReportsDiagnostic()
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
        Assert.Contains("ALE0011", ids);
    }

    // ALE0012: [Filter<T>] で T が ILambdaFilter を実装していない
    [Fact]
    public void ALE0012_WhenFilterTypeNotImplementILambdaFilter_ReportsDiagnostic()
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
        Assert.Contains("ALE0012", ids);
    }
}
