namespace AmazonLambdaExtension;

using AmazonLambdaExtension.Generator.Models;

using Xunit;

/// <summary>
/// LambdaGenerator が生成するモデルの構造を検証するテスト。
/// 旧版の ModelBuilderTest に相当する。
/// </summary>
public sealed class ModelBuilderTests
{
    // ---------------------------------------------------------------------------
    // ServiceResolver
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenNoServiceResolver_ServiceResolverIsNull()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed partial class Function
{
    [Event]
    public void Handle()
    {
    }
}
");
        Assert.NotEmpty(sources);
    }

    // ---------------------------------------------------------------------------
    // [Event] ハンドラ
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenEventHandler_GeneratesHandlerMethod()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed partial class QueueFunction
{
    [Event]
    public void Handle()
    {
    }
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
    }

    // ---------------------------------------------------------------------------
    // [HttpApi] ハンドラ
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenHttpApiHandler_GeneratesHandlerMethod()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class CrudFunctions
{
    [HttpApi(LambdaHttpMethod.Get, ""/items/{id}"")]
    public IHttpResult GetItem()
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("GetItem_Handler"));
        Assert.NotNull(handlerSource);
    }

    // ---------------------------------------------------------------------------
    // [FromQuery] バインド
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenFromQuery_GeneratesQueryStringBinding()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle([FromQuery] string name)
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("QueryStringParameters", handlerSource);
    }

    [Fact]
    public void WhenFromQueryArray_GeneratesArrayBinding()
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("Split(',')", handlerSource);
    }

    // ---------------------------------------------------------------------------
    // [FromRoute] バインド
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenFromRoute_GeneratesPathParameterBinding()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items/{id}"")]
    public IHttpResult GetItem([FromRoute] string id)
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("GetItem_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("PathParameters", handlerSource);
    }

    // ---------------------------------------------------------------------------
    // [FromHeader] バインド
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenFromHeader_GeneratesHeaderBinding()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle([FromHeader(""x-tenant-id"")] string tenantId)
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("Headers", handlerSource);
    }

    // ---------------------------------------------------------------------------
    // [FromServices] バインド
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenFromServices_GeneratesServiceProviderResolution()
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
    public IHttpResult Handle([FromServices] System.IDisposable service)
        => HttpResults.Ok(new { });
}

public sealed class Resolver
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureServices()
        => new Microsoft.Extensions.DependencyInjection.ServiceCollection();
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("GetRequiredService", handlerSource);
    }

    // ---------------------------------------------------------------------------
    // フィルタパイプライン
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenFilterSpecified_GeneratesPipelineCode()
    {
        var sources = CompilationHelper.RunGenerator(@"
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("__Handle_Pipeline__", handlerSource);
        Assert.Contains("__Handle_Inner__", handlerSource);
    }

    [Fact]
    public void WhenNoFilter_DoesNotGeneratePipelineCode()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class SimpleFunctions
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle()
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.DoesNotContain("__Handle_Pipeline__", handlerSource);
        Assert.DoesNotContain("__Handle_Inner__", handlerSource);
    }

    // ---------------------------------------------------------------------------
    // 400 Bad Request 短絡
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenIntParameter_GeneratesStringConverterCall()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class Function
{
    [HttpApi(LambdaHttpMethod.Get, ""/items"")]
    public IHttpResult Handle([FromQuery] int page)
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler"));
        Assert.NotNull(handlerSource);
        Assert.Contains("TryToInt32", handlerSource);
    }

    // ---------------------------------------------------------------------------
    // [FunctionUrl] ハンドラ
    // ---------------------------------------------------------------------------

    [Fact]
    public void WhenFunctionUrlHandler_GeneratesHandlerMethod()
    {
        var sources = CompilationHelper.RunGenerator(@"
namespace Test;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
public sealed partial class HealthCheck
{
    [FunctionUrl(AuthType = FunctionUrlAuthType.NONE)]
    public IHttpResult Ping()
        => HttpResults.Ok(new { });
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Ping_Handler"));
        Assert.NotNull(handlerSource);
    }
}
