namespace AmazonLambdaExtension.TargetProject;

using AmazonLambdaExtension.Serialization;
using AmazonLambdaExtension.TargetProject.Components.Logging;

using Microsoft.Extensions.Logging;

public sealed class ServiceLocator : IDisposable
{
    private readonly ILoggerFactory loggerFactory = new LambdaLoggerFactory(LogLevel.Information, null);

    public void Dispose()
    {
        loggerFactory.Dispose();
    }

    public ILogger<T> CreateLogger<T>() => loggerFactory.CreateLogger<T>();

    public static ICalculator ResolveCalculator() => new Calculator();

    // TODO
    public IBodySerializer ResolveSerializer() => JsonBodySerializer.Default;

    // TODO
    public T GetService<T>() => default!;
}

public interface ICalculator
{
    public int Add(int x, int y);
}

public class Calculator : ICalculator
{
    public int Add(int x, int y) => x + y;
}
