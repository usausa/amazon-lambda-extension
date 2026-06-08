namespace AmazonLambdaExtension;

using Xunit;

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
    public void HttpApiHandler_IHttpResultReturn_NoFilter_GeneratesResponseReturn()
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

        // APIGatewayHttpApiV2ProxyResponse を返す
        Assert.Contains("global::Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse", handlerSource, StringComparison.Ordinal);
        // IHttpResult.ToResponse を呼ぶ
        Assert.Contains(".ToResponse(", handlerSource, StringComparison.Ordinal);
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

        // パイプライン構造を持つ（invocation ごとにインライン構築）
        Assert.Contains("__pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
        // invocation ごとに DI scope を作成
        Assert.Contains("CreateAsyncScope(__provider__)", handlerSource, StringComparison.Ordinal);
        // フィルタは scope から解決される
        Assert.Contains("__filter0__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<global::Test.LoggingFilter>(__sp__)", handlerSource, StringComparison.Ordinal);
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
        // provider は BuildProvider() 経由で、Debug ビルドのみ scope 検証を有効化
        Assert.Contains("BuildProvider()", sharedSource, StringComparison.Ordinal);
        Assert.Contains("ValidateScopes", sharedSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // DI でもフィルタ・[FromServices] が無ければ scope を生成しない
    // ---------------------------------------------------------------------------

    [Fact]
    public void DiHandler_NoFilterNoFromServices_DoesNotCreateScope()
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
    public IHttpResult Handle([FromQuery] int page)
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

        // フィルタも [FromServices] も無い DI ハンドラは invocation scope を作らない（本体は singleton）
        Assert.DoesNotContain("CreateAsyncScope", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [FromBody] + validation → __requestValidator__ が shared フィールドに生成される
    // ---------------------------------------------------------------------------

    [Fact]
    public void FromBody_WithValidation_GeneratesRequestValidatorField()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

public sealed class Input { public string? Name { get; set; } }

[Lambda]
[ServiceResolver(typeof(Resolver))]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Post, ""/items"")]
    public IHttpResult Create([FromBody] Input input)
        => HttpResults.Ok(new { });
}

public sealed class Resolver
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
}
");
        var sharedSource = sources.Values.FirstOrDefault(s => s.Contains("__provider__", StringComparison.Ordinal));
        Assert.NotNull(sharedSource);
        output.WriteLine(sharedSource);

        // __requestValidator__ が shared フィールドとして生成される
        Assert.Contains("__requestValidator__", sharedSource, StringComparison.Ordinal);
        Assert.Contains("global::AmazonLambdaExtension.Validation.IRequestValidator", sharedSource, StringComparison.Ordinal);

        var handlerSource = sources.Values.Single(s => s.Contains("Create_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        // validation 呼び出しが IRequestValidator ベース
        Assert.Contains("__requestValidator__.Validate", handlerSource, StringComparison.Ordinal);
        // ValidationHelper への直接参照は存在しない
        Assert.DoesNotContain("ValidationHelper", handlerSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------
    // [FromBody(SkipValidate = true)] → __requestValidator__ が生成されない
    // ---------------------------------------------------------------------------

    [Fact]
    public void FromBody_SkipValidate_DoesNotGenerateRequestValidatorField()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

public sealed class Input { public string? Name { get; set; } }

[Lambda]
[ServiceResolver(typeof(Resolver))]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Post, ""/items"")]
    public IHttpResult Create([FromBody(SkipValidate = true)] Input input)
        => HttpResults.Ok(new { });
}

public sealed class Resolver
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
}
");
        var sharedSource = sources.Values.FirstOrDefault(s => s.Contains("__provider__", StringComparison.Ordinal));
        Assert.NotNull(sharedSource);
        output.WriteLine(sharedSource);

        // SkipValidate = true の場合 __requestValidator__ は生成されない
        Assert.DoesNotContain("__requestValidator__", sharedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void FromBody_WithoutServiceResolver_GeneratesDefaultSerializerAndValidator()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

public sealed class Input { public string? Name { get; set; } }

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Post, ""/items"")]
    public IHttpResult Create([FromBody] Input input)
        => HttpResults.Ok(input);
}
");
        var sharedSource = sources.Values.FirstOrDefault(s => s.Contains("__bodySerializer__", StringComparison.Ordinal));
        Assert.NotNull(sharedSource);
        output.WriteLine(sharedSource);

        Assert.Contains("JsonBodySerializer.Default", sharedSource, StringComparison.Ordinal);
        Assert.Contains("new global::AmazonLambdaExtension.Validation.DataAnnotationsRequestValidator()", sharedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void EventHandler_WithFromServices_NoFilter_GeneratesServiceBinding()
    {
        var sources = CompilationHelper.RunGenerator(@"
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
");
        var handlerSource = sources.Values.Single(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("CreateAsyncScope(__provider__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<global::Test.IProcessor>(__sp__)", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__target__.Handle(ev, p1!)", handlerSource, StringComparison.Ordinal);
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
        Assert.Contains(".ToSimpleResponse(", handlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthorizerHandler_WithCustomAuthorizerRequest_UsesRouteArn()
    {
        var sources = CompilationHelper.RunGenerator(@"
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
");
        var handlerSource = sources.Values.Single(s => s.Contains("Authorize_Handler", StringComparison.Ordinal));
        output.WriteLine(handlerSource);

        Assert.Contains("APIGatewayCustomAuthorizerV2Request", handlerSource, StringComparison.Ordinal);
        Assert.Contains(".ToIamResponse(request.RouteArn)", handlerSource, StringComparison.Ordinal);
    }
}
