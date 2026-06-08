namespace AmazonLambdaExtension.Example.Tests;

using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using AmazonLambdaExtension.Example;

public class QueueProcessorHandlerTests
{
    [Fact]
    public Task Handle_Handler_ProcessesRecords_WithoutException()
    {
        var ev = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage { MessageId = "msg-1", Body = "hello" },
                new SQSEvent.SQSMessage { MessageId = "msg-2", Body = "world" }
            ]
        };
        var ctx = new TestLambdaContext();

        return QueueProcessor.Handle_Handler(ev, ctx);
    }
}
