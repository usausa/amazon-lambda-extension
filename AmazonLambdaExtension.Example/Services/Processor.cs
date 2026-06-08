namespace AmazonLambdaExtension.Example.Services;

public interface IProcessor
{
    ValueTask HandleAsync(string body);
}

public sealed class Processor : IProcessor
{
    public ValueTask HandleAsync(string body)
    {
        return default;
    }
}
