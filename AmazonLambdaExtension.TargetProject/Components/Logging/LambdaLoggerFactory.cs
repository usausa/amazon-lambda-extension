namespace AmazonLambdaExtension.TargetProject.Components.Logging;

using Microsoft.Extensions.Logging;

public sealed class LambdaLoggerFactory : ILoggerFactory
{
    private readonly LogLevel defaultLevel;

    private readonly IDictionary<string, LogLevel>? levels;

    public LambdaLoggerFactory(LogLevel defaultLevel, IDictionary<string, LogLevel>? levels)
    {
        this.defaultLevel = defaultLevel;
        this.levels = levels;
    }

    public void Dispose()
    {
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new LambdaLogger(categoryName, (levels is not null && levels.TryGetValue(categoryName, out var level)) ? level : defaultLevel);
    }
}
