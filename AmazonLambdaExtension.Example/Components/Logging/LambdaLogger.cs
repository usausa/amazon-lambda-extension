namespace AmazonLambdaExtension.Example.Components.Logging;

using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

public sealed class LambdaLogger : ILogger
{
    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private readonly string categoryName;

    private readonly LogLevel threshold;

    public LambdaLogger(string categoryName, LogLevel threshold)
    {
        this.categoryName = categoryName;
        this.threshold = threshold;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= threshold;

    public IDisposable BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel >= threshold)
        {
            Amazon.Lambda.Core.LambdaLogger.Log($"{LogLevelFormat(logLevel)} {LambdaLoggerContext.RequestId} ({categoryName}) - {formatter(state, exception)}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string LogLevelFormat(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "🟦 [TRACE]",
            LogLevel.Debug => "🟪 [DEBUG]",
            LogLevel.Information => "🟩 [INFO]",
            LogLevel.Warning => "🟧 [WARN]",
            LogLevel.Error => "🟥 [ERROR[",
            LogLevel.Critical => "⬛ [CRITICAL]",
            _ => "NONE"
        };
    }
}
