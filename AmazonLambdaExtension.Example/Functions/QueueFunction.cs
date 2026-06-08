namespace AmazonLambdaExtension.Example.Functions;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.Example.Services;

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class QueueFunction
{
    private readonly IProcessor processor;

    public QueueFunction(IProcessor processor)
    {
        this.processor = processor;
    }

    [Event]
    public async ValueTask Handle(SQSEvent ev, ILambdaContext context)
    {
        foreach (var record in ev.Records)
        {
            context.Logger.LogInformation("Processing {MessageId}", record.MessageId);
            await processor.HandleAsync(record.Body);
        }
    }
}
