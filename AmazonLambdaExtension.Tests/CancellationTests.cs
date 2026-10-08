namespace AmazonLambdaExtension;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public sealed class CancellationTests
{
    private const string Head =
        """
        namespace Test;

        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using AmazonLambdaExtension;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;
        using AmazonLambdaExtension.Filters;
        using Microsoft.Extensions.DependencyInjection;

        public static class State
        {
            public static string Of(CancellationToken token) =>
                !token.CanBeCanceled ? "none" : token.IsCancellationRequested ? "canceled" : "active";
        }

        public sealed class PassFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
        }

        public sealed class TokenFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
            {
                context.Result = HttpResults.Ok("filter-" + State.Of(context.CancellationToken));
                return ValueTask.CompletedTask;
            }
        }

        """;

    //--------------------------------------------------------------------------------
    // Handler and filter
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task HandlerReceivesActiveToken(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + $$"""
            [Lambda]
            {{filter}}
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get(CancellationToken token) => HttpResults.Ok(State.Of(token));
            }
            """);

        var response = await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal("active", response.Body);
    }

    [Fact]
    public async Task FilterReceivesActiveToken()
    {
        var assembly = CompilationHelper.LoadAssembly(Head + """
            [Lambda]
            [Filter<TokenFilter>]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();
            }
            """);

        var response = await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal("filter-active", response.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Filter<PassFilter>]")]
    public async Task MarginLongerThanRemainingTimeCancelsToken(string filter)
    {
        var assembly = CompilationHelper.LoadAssembly(Head + $$"""
            public static class Resolver
            {
                public static IServiceCollection ConfigureServices()
                {
                    var services = new ServiceCollection();
                    services.AddSingleton<Amazon.Lambda.Core.ILambdaSerializer>(new Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer());
                    services.AddSingleton(new LambdaCancellationOptions { Margin = TimeSpan.FromMinutes(2) });
                    services.AddSingleton<PassFilter>();
                    return services;
                }
            }

            [Lambda]
            [ServiceResolver(typeof(Resolver))]
            {{filter}}
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get(CancellationToken token) => HttpResults.Ok(State.Of(token));
            }
            """);

        var response = await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest());

        Assert.Equal("canceled", response.Body);
    }

    [Fact]
    public void EventHandlerCanTakeToken()
    {
        var source =
            """
            namespace Test;

            using System.Threading;
            using System.Threading.Tasks;
            using Amazon.Lambda.SQSEvents;
            using AmazonLambdaExtension.Annotations;

            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public Task Handle(SQSEvent ev, CancellationToken token) => Task.CompletedTask;
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void HandlerWithoutTokenAndFiltersDoesNotCreateToken()
    {
        var result = CompilationHelper.RunGenerator(
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);

        Assert.DoesNotContain(result.Sources.Values, static s => s.Contains("__cts__", StringComparison.Ordinal));
    }

    //--------------------------------------------------------------------------------
    // LambdaCancellationOptions
    //--------------------------------------------------------------------------------

    [Fact]
    public void ZeroRemainingTimeDoesNotCancel()
    {
        using var source = LambdaCancellationOptions.Default.CreateTokenSource(new StubContext(TimeSpan.Zero));

        Assert.False(source.Token.IsCancellationRequested);
    }

    [Fact]
    public void RemainingTimeLongerThanMarginDoesNotCancelYet()
    {
        using var source = LambdaCancellationOptions.Default.CreateTokenSource(new StubContext(TimeSpan.FromMinutes(1)));

        Assert.False(source.Token.IsCancellationRequested);
    }

    [Fact]
    public void RemainingTimeShorterThanMarginCancelsImmediately()
    {
        using var source = LambdaCancellationOptions.Default.CreateTokenSource(new StubContext(TimeSpan.FromMilliseconds(100)));

        Assert.True(source.Token.IsCancellationRequested);
    }

    private sealed class StubContext(TimeSpan remainingTime) : ILambdaContext
    {
        public string AwsRequestId => "request";

        public IClientContext ClientContext => null!;

        public string FunctionName => "function";

        public string FunctionVersion => "1";

        public ICognitoIdentity Identity => null!;

        public string InvokedFunctionArn => "arn";

        public ILambdaLogger Logger => null!;

        public string LogGroupName => "group";

        public string LogStreamName => "stream";

        public int MemoryLimitInMB => 128;

        public TimeSpan RemainingTime => remainingTime;
    }
}
