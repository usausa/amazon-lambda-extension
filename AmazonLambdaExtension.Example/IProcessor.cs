namespace AmazonLambdaExtension.Example;

public interface IProcessor
{
    ValueTask HandleAsync(string body);
}

public sealed class MockProcessor : IProcessor
{
    public ValueTask HandleAsync(string body)
    {
        return default;
    }
}
