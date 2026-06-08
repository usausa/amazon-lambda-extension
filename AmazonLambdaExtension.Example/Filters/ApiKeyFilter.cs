namespace AmazonLambdaExtension.Example.Filters;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.APIGateway;
using AmazonLambdaExtension.Filters;

public sealed class ApiKeyFilter : ILambdaFilter
{
    public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
    {
        var req = context.GetRequest<APIGatewayHttpApiV2ProxyRequest>();
        if (!req.Headers.TryGetValue("x-api-key", out var key) || key != "expected")
        {
            context.Result = HttpResults.Unauthorized();
            return default;
        }

        return next(context);
    }
}
