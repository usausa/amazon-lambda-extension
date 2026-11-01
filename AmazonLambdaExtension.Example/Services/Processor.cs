namespace AmazonLambdaExtension.Example.Services;

public interface IProcessor
{
    ValueTask HandleAsync(string body);
}

public sealed class Processor : IProcessor
{
    private static readonly Action<ILogger, int, Exception?> MessageHandled =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, nameof(HandleAsync)), "Message handled. length=[{Length}]");

    private readonly ILogger<Processor> log;

    public Processor(ILogger<Processor> log)
    {
        this.log = log;
    }

    public ValueTask HandleAsync(string body)
    {
        MessageHandled(log, body.Length, null);
        return default;
    }
}
