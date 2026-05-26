namespace AmazonLambdaExtension;

using Xunit;
using Xunit.Sdk;

// LambdaGenerator が想定どおりのコードを生成することを確認するスナップショットテスト。
// 生成コードのシグネチャ・構造を検証する。
public sealed class GeneratorSnapshotTests(ITestOutputHelper output)
{
    // ---------------------------------------------------------------------------
    // [Event] フィルタなし
    // ---------------------------------------------------------------------------

    [Fact]
    public void EventHandler_NoFilter_GeneratesCorrectStructure()
    {
        var sources = CompilationHelper.RunGenerator(@"
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
");
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        // エントリポイントは public static で正しい引数型を持つ
        Assert.Contains("public static", handlerSource, StringComparison.Ordinal);
        Assert.Contains("Handle_Handler", handlerSource, StringComparison.Ordinal);
        Assert.Contains("global::Amazon.Lambda.SQSEvents.SQSEvent", handlerSource, StringComparison.Ordinal);
        Assert.Contains("global::Amazon.Lambda.Core.ILambdaContext", handlerSource, StringComparison.Ordinal);
        // Event ハンドラは例外を throw する
        Assert.Contains("throw;", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [HttpApi] + IHttpResult 戻り値 + フィルタなし
    // ---------------------------------------------------------------------------

    [Fact]
    public void HttpApiHandler_IHttpResultReturn_NoFilter_GeneratesStreamReturn()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle()
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        // Stream を返す
        Assert.Contains("global::System.IO.Stream", handlerSource, StringComparison.Ordinal);
        // IHttpResult.Serialize を呼ぶ
        Assert.Contains(".Serialize(", handlerSource, StringComparison.Ordinal);
        // ApiException をキャッチする
        Assert.Contains("global::AmazonLambdaExtension.ApiException", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [HttpApi] + FromRoute / FromQuery バインド
    // ---------------------------------------------------------------------------

    [Fact]
    public void HttpApiHandler_FromRouteAndQuery_GeneratesCorrectBinding()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items/{id}"")]
    public IHttpResult GetItem([FromRoute] string id, [FromQuery] int page)
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.Single(s => s.Contains("GetItem_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        // PathParameters から id をバインド
        Assert.Contains("PathParameters", handlerSource, StringComparison.Ordinal);
        Assert.Contains(@"""id""", handlerSource, StringComparison.Ordinal);
        // QueryStringParameters から page をバインド
        Assert.Contains("QueryStringParameters", handlerSource, StringComparison.Ordinal);
        Assert.Contains(@"""page""", handlerSource, StringComparison.Ordinal);
        // int への変換
        Assert.Contains("TryToInt32", handlerSource, StringComparison.Ordinal);
        // 変換失敗時 BadRequest 返却
        Assert.Contains("BadRequest", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [HttpApi] + FromQuery 配列
    // ---------------------------------------------------------------------------

    [Fact]
    public void HttpApiHandler_FromQueryArray_GeneratesArrayBinding()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle([FromQuery] int[] ids)
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        // カンマ区切りで Split
        Assert.Contains("Split(',')", handlerSource, StringComparison.Ordinal);
        // 配列生成
        Assert.Contains("new int[", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [HttpApi] + フィルタあり
    // ---------------------------------------------------------------------------

    [Fact]
    public void HttpApiHandler_WithFilter_GeneratesPipelineStructure()
    {
        var sources = CompilationHelper.RunGenerator(@"
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
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle()
        => HttpResults.Ok(new { });
}

public sealed class Resolver
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
}
");
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        // パイプライン構造を持つ
        Assert.Contains("__Handle_Pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("BuildHandlePipeline()", handlerSource, StringComparison.Ordinal);
        // フィルタは DI から解決される
        Assert.Contains("__filter0__", handlerSource, StringComparison.Ordinal);
        // Inner では ctx.Result にセット
        Assert.Contains("ctx.Result", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // static フィールド（コールドスタート時初期化）
    // ---------------------------------------------------------------------------

    [Fact]
    public void SharedFields_GeneratesStaticReadonlyProvider()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
[ServiceResolver(typeof(Resolver))]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle()
        => HttpResults.Ok(new { });
}

public sealed class Resolver
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
}
");
        // 共有フィールドソース（__Shared__.g.cs など）
        var sharedSource = sources.Values.FirstOrDefault(s => s.Contains("__provider__", StringComparison.Ordinal));
        Assert.NotNull(sharedSource);
        output.WriteLine(sharedSource);

        Assert.Contains("static readonly", sharedSource, StringComparison.Ordinal);
        Assert.Contains("__provider__", sharedSource, StringComparison.Ordinal);
        Assert.Contains("__target__", sharedSource, StringComparison.Ordinal);
        Assert.Contains("BuildServiceProvider(", sharedSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [HttpApiAuthorizer]
    // ---------------------------------------------------------------------------

    [Fact]
    public void AuthorizerHandler_GeneratesCorrectStructure()
    {
        var sources = CompilationHelper.RunGenerator(@"
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
");
        var handlerSource = sources.Values.Single(s => s.Contains("Authorize_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("Authorize_Handler", handlerSource, StringComparison.Ordinal);
        Assert.Contains("AuthorizerResult", handlerSource, StringComparison.Ordinal);
        Assert.Contains(".Serialize(", handlerSource, StringComparison.Ordinal);
    }
}
