namespace AmazonLambdaExtension.Example.Components.Logging;

using System.Collections;

using Microsoft.Extensions.Logging;

public static class LoggingBuilderExtensions
{
    public static void AddLambdaLogger(this ILoggingBuilder builder)
    {
        var defaultValue = Environment.GetEnvironmentVariable("LogLevel");
        var defaultLevel = !String.IsNullOrEmpty(defaultValue) && Enum.TryParse(defaultValue, out LogLevel result)
            ? result
            : LogLevel.Information;
        var logLevels = Environment.GetEnvironmentVariables()
            .OfType<DictionaryEntry>()
            .Where(static x => (x.Key is string key) && key.StartsWith("LogLevel_", StringComparison.Ordinal))
            .ToDictionary(static x => ((string)x.Key)[9..].Replace('_', '.'), x => Enum.TryParse(x.Value as string, out result) ? result : defaultLevel);
#pragma warning disable CA2000
        builder.AddProvider(new LambdaLoggerProvider(defaultLevel, logLevels.Count == 0 ? null : logLevels));
#pragma warning restore CA2000
    }
}
