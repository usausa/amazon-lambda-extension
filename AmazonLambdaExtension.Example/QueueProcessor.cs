namespace AmazonLambdaExtension.Example;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using AmazonLambdaExtension.Annotations;

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class QueueProcessor
{
    private readonly IProcessor processor;

    public QueueProcessor(IProcessor processor)
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
