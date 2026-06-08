namespace AmazonLambdaExtension.Filters;

using System.Threading;

using Amazon.Lambda.Core;

#pragma warning disable CA1721
public sealed class LambdaInvocationContext
{
    public object Request { get; init; } = default!;

    public ILambdaContext LambdaContext { get; init; } = default!;

    public IServiceProvider ServiceProvider { get; init; } = default!;

    public CancellationToken CancellationToken { get; init; }

    public object? Result { get; set; }

    private Dictionary<string, object?>? items;

    public IDictionary<string, object?> Items => items ??= [];

    public TRequest GetRequest<TRequest>()
        where TRequest : class
        => (TRequest)Request;
}
#pragma warning restore CA1721
