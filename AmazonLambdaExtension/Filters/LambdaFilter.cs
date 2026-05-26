namespace AmazonLambdaExtension.Filters;

using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

#pragma warning disable CA1711
public delegate ValueTask LambdaFilterDelegate(LambdaInvocationContext context);
#pragma warning restore CA1711

public interface ILambdaFilter
{
    ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next);
}

public sealed class LambdaInvocationContext
{
    public object Request { get; init; } = default!;

    public ILambdaContext LambdaContext { get; init; } = default!;

    public CancellationToken CancellationToken { get; init; }

    public object? Result { get; set; }

    private Dictionary<string, object?>? items;

    public IDictionary<string, object?> Items =>
        items ??= [];

#pragma warning disable CA1721
    public TRequest GetRequest<TRequest>()
        where TRequest : class
        => (TRequest)Request;
#pragma warning restore CA1721
}
