namespace AmazonLambdaExtension.Example.Components.Logging;

public static class LambdaLoggerContext
{
    private static readonly AsyncLocal<string?> RequestIdStore = new();

    public static string? RequestId
    {
        get => RequestIdStore.Value;
        set => RequestIdStore.Value = value;
    }
}
