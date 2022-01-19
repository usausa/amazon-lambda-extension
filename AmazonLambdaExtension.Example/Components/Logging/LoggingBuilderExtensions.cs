namespace AmazonLambdaExtension.Example.Components.Logging;

using System.Collections;

using Microsoft.Extensions.Logging;

public static class LoggingBuilderExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore")]
    public static void AddLambdaLogger(this ILoggingBuilder builder)
    {
        var defaultValue = Environment.GetEnvironmentVariable("LogLevel");
        var defaultLevel = !String.IsNullOrEmpty(defaultValue) && Enum.TryParse(defaultValue, out LogLevel result)
            ? result
            : LogLevel.Information;
        var logLevels = Environment.GetEnvironmentVariables()
            .OfType<DictionaryEntry>()
            .Where(x => (x.Key is string key) && key.StartsWith("LogLevel_", StringComparison.Ordinal))
            .ToDictionary(x => ((string)x.Key)[9..].Replace('_', '.'), x => Enum.TryParse(x.Value as string, out result) ? result : defaultLevel);
        builder.AddProvider(new LambdaLoggerProvider(defaultLevel, logLevels.Count == 0 ? null : logLevels));
    }
}
