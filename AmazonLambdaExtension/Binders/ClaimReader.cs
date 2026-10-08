namespace AmazonLambdaExtension.Binders;

using System.Diagnostics.CodeAnalysis;

public static class ClaimReader
{
    // Claims validated by the HTTP API JWT authorizer (requestContext.authorizer.jwt.claims)
    public static bool TryGetValue(IDictionary<string, string>? claims, string name, [NotNullWhen(true)] out string? value)
    {
        value = (claims is not null) && claims.TryGetValue(name, out var raw) ? raw : null;
        return value is not null;
    }
}
