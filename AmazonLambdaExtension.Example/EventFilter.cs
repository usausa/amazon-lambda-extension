namespace AmazonLambdaExtension.Example;

using Amazon.Lambda.Core;

using AmazonLambdaExtension.Example.Components.Logging;

#pragma warning disable IDE0060
public sealed class EventFilter
{
    public void OnFunctionExecuting(ILambdaContext context)
    {
        LambdaLoggerContext.RequestId = context.AwsRequestId;
    }

    public void OnFunctionExecuted(ILambdaContext context)
    {
        LambdaLoggerContext.RequestId = null;
    }
}
#pragma warning restore IDE0060
