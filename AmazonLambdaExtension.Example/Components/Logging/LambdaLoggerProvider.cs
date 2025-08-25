namespace AmazonLambdaExtension.Example.Components.Logging;

using Microsoft.Extensions.Logging;

public sealed class LambdaLoggerProvider : ILoggerProvider
{
    private readonly LogLevel defaultLevel;

    private readonly IDictionary<string, LogLevel>? levels;

    public LambdaLoggerProvider(LogLevel defaultLevel, IDictionary<string, LogLevel>? levels)
    {
        this.defaultLevel = defaultLevel;
        this.levels = levels;
    }

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new LambdaLogger(categoryName, (levels is not null && levels.TryGetValue(categoryName, out var level)) ? level : defaultLevel);
    }
}
