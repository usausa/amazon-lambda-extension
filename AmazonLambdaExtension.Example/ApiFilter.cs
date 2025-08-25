namespace AmazonLambdaExtension.Example;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using AmazonLambdaExtension.Example.Components.Logging;

#pragma warning disable IDE0060
public sealed class ApiFilter
{
    public APIGatewayProxyResponse? OnFunctionExecuting(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.Headers?.ContainsKey("X-Lambda-Ping") ?? false)
        {
            return new APIGatewayProxyResponse { StatusCode = 200 };
        }

        LambdaLoggerContext.RequestId = context.AwsRequestId;

        return null;
    }

    public void OnFunctionExecuted(APIGatewayProxyRequest request, ILambdaContext context)
    {
        LambdaLoggerContext.RequestId = null;
    }
}
#pragma warning restore IDE0060
