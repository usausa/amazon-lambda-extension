namespace AmazonLambdaExtension.Example;

using System.Diagnostics;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.APIGateway;
using AmazonLambdaExtension.Filters;

using Microsoft.Extensions.Logging;

public sealed class LoggingFilter : ILambdaFilter
{
    private readonly ILogger<LoggingFilter> logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        this.logger = logger;
    }

    public async ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next)
    {
        var requestId = ctx.LambdaContext.AwsRequestId;
        logger.LogInformation("Begin {RequestId}", requestId);
        var sw = Stopwatch.StartNew();
        try
        {
            await next(ctx);
        }
        finally
        {
            logger.LogInformation("End {RequestId} ({Ms}ms)", requestId, sw.ElapsedMilliseconds);
        }
    }
}

public sealed class ApiKeyFilter : ILambdaFilter
{
    public ValueTask InvokeAsync(LambdaInvocationContext ctx, LambdaFilterDelegate next)
    {
        var req = ctx.GetRequest<APIGatewayHttpApiV2ProxyRequest>();
        if (!req.Headers.TryGetValue("x-api-key", out var key) || key != "expected")
        {
            ctx.Result = HttpResults.Unauthorized();
            return default;
        }

        return next(ctx);
    }
}
