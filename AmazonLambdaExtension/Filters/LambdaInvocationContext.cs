namespace AmazonLambdaExtension.Filters;

using System.Security.Claims;
using System.Threading;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public sealed class LambdaInvocationContext
{
    public object Request { get; init; } = default!;

    public ILambdaContext LambdaContext { get; init; } = default!;

    public IServiceProvider ServiceProvider { get; init; } = default!;

    public CancellationToken CancellationToken { get; init; }

    public LambdaClaimsOptions ClaimsOptions { get; init; } = LambdaClaimsOptions.Default;

    public object? Result { get; set; }

    public ClaimsPrincipal User
    {
        get => field ??= ClaimsOptions.CreatePrincipal((Request as APIGatewayHttpApiV2ProxyRequest)?.RequestContext?.Authorizer?.Jwt?.Claims);
        set;
    }

    private Dictionary<string, object?>? items;

    public IDictionary<string, object?> Items => items ??= [];

    public TRequest GetRequestAs<TRequest>()
        where TRequest : class
        => (TRequest)Request;
}
