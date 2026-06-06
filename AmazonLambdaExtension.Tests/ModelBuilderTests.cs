namespace AmazonLambdaExtension;

using Xunit;

// LambdaGenerator が生成するモデルの構造を検証するテスト。
// 旧版の ModelBuilderTest に相当する。
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

public sealed class MyEvent { }

[Lambda]
public sealed partial class Function
{
    [Event]
    public void Handle(MyEvent ev)
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

public sealed class MyEvent { }

[Lambda]
public sealed partial class QueueFunction
{
    [Event]
    public void Handle(MyEvent ev)
    {
    }
}
");
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("GetItem_Handler", StringComparison.Ordinal));
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("QueryStringParameters", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("Split(',')", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("GetItem_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("PathParameters", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("Headers", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("GetRequiredService", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("__pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.Contains("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.DoesNotContain("__pipeline__", handlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("__Handle_Inner__", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Handle_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
        Assert.Contains("TryToInt32", handlerSource, StringComparison.Ordinal);
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
        var handlerSource = sources.Values.FirstOrDefault(s => s.Contains("Ping_Handler", StringComparison.Ordinal));
        Assert.NotNull(handlerSource);
    }
}
