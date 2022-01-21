namespace AmazonLambdaExtension.Example;

using Amazon.Lambda.Core;

using AmazonLambdaExtension.Example.Components.Logging;

public sealed class EventFilter
{
    public void OnFunctionExecuting(ILambdaContext context)
    {
        LambdaLoggerContext.RequestId = context.AwsRequestId;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Ignore")]
    public void OnFunctionExecuted(ILambdaContext context)
    {
        LambdaLoggerContext.RequestId = null;
    }
}
