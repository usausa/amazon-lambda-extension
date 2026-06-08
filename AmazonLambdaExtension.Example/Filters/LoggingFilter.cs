namespace AmazonLambdaExtension.Example.Filters;

using System.Diagnostics;

using AmazonLambdaExtension.Filters;

using Microsoft.Extensions.Logging;

public sealed class LoggingFilter : ILambdaFilter
{
    private readonly ILogger<LoggingFilter> logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        this.logger = logger;
    }

    public async ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
    {
        var requestId = context.LambdaContext.AwsRequestId;
        logger.LogInformation("Begin {RequestId}", requestId);
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation("End {RequestId} ({Ms}ms)", requestId, sw.ElapsedMilliseconds);
        }
    }
}
