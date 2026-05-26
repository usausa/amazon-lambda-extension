namespace AmazonLambdaExtension;

using Xunit;
using Xunit.Sdk;

/// <summary>
/// LambdaGenerator が想定どおりのコードを生成することを確認するスナップショットテスト。
/// 生成コードのシグネチャ・構造を検証する。
/// </summary>
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
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler"));
        output.WriteLine(handlerSource);

        // エントリポイントは public static で正しい引数型を持つ
        Assert.Contains("public static", handlerSource);
        Assert.Contains("Handle_Handler", handlerSource);
        Assert.Contains("global::Amazon.Lambda.SQSEvents.SQSEvent", handlerSource);
        Assert.Contains("global::Amazon.Lambda.Core.ILambdaContext", handlerSource);
        // Event ハンドラは例外を throw する
        Assert.Contains("throw;", handlerSource);
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
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler"));
        output.WriteLine(handlerSource);

        // Stream を返す
        Assert.Contains("global::System.IO.Stream", handlerSource);
        // IHttpResult.Serialize を呼ぶ
        Assert.Contains(".Serialize(", handlerSource);
        // ApiException をキャッチする
        Assert.Contains("global::AmazonLambdaExtension.ApiException", handlerSource);
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
        var handlerSource = sources.Values.Single(s => s.Contains("GetItem_Handler"));
        output.WriteLine(handlerSource);

        // PathParameters から id をバインド
        Assert.Contains("PathParameters", handlerSource);
        Assert.Contains(@"""id""", handlerSource);
        // QueryStringParameters から page をバインド
        Assert.Contains("QueryStringParameters", handlerSource);
        Assert.Contains(@"""page""", handlerSource);
        // int への変換
        Assert.Contains("TryToInt32", handlerSource);
        // 変換失敗時 BadRequest 返却
        Assert.Contains("BadRequest", handlerSource);
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
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler"));
        output.WriteLine(handlerSource);

        // カンマ区切りで Split
        Assert.Contains("Split(',')", handlerSource);
        // 配列生成
        Assert.Contains("new int[", handlerSource);
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
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler"));
        output.WriteLine(handlerSource);

        // パイプライン構造を持つ
        Assert.Contains("__Handle_Pipeline__", handlerSource);
        Assert.Contains("__Handle_Inner__", handlerSource);
        Assert.Contains("BuildHandlePipeline()", handlerSource);
        // フィルタは DI から解決される
        Assert.Contains("__filter0__", handlerSource);
        // Inner では ctx.Result にセット
        Assert.Contains("ctx.Result", handlerSource);
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
        var sharedSource = sources.Values.FirstOrDefault(s => s.Contains("__provider__"));
        Assert.NotNull(sharedSource);
        output.WriteLine(sharedSource);

        Assert.Contains("static readonly", sharedSource);
        Assert.Contains("__provider__", sharedSource);
        Assert.Contains("__target__", sharedSource);
        Assert.Contains("BuildServiceProvider(", sharedSource);
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
        var handlerSource = sources.Values.Single(s => s.Contains("Authorize_Handler"));
        output.WriteLine(handlerSource);

        Assert.Contains("Authorize_Handler", handlerSource);
        Assert.Contains("AuthorizerResult", handlerSource);
        Assert.Contains(".Serialize(", handlerSource);
    }
}
