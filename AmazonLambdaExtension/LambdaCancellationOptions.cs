namespace AmazonLambdaExtension;

using System.Threading;

using Amazon.Lambda.Core;

// Settings of the CancellationToken passed to filters and handlers.
// Register it as a singleton in ServiceResolver to change the margin; otherwise Default is used.
public sealed class LambdaCancellationOptions
{
    public static LambdaCancellationOptions Default { get; } = new();

    // Time left for the handler to stop and return after the token is canceled
    public TimeSpan Margin { get; init; } = TimeSpan.FromMilliseconds(500);

    public CancellationTokenSource CreateTokenSource(ILambdaContext context)
    {
        var remaining = context.RemainingTime;

        // RemainingTime is zero outside Lambda (for example, a test context), so the token is never canceled then
        if (remaining <= TimeSpan.Zero)
        {
            return new CancellationTokenSource();
        }

        var timeout = remaining - Margin;
        if (timeout > TimeSpan.Zero)
        {
            return new CancellationTokenSource(timeout);
        }

        var source = new CancellationTokenSource();
        source.Cancel();
        return source;
    }
}
