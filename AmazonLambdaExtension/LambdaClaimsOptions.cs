namespace AmazonLambdaExtension;

using System.Security.Claims;

public sealed class LambdaClaimsOptions
{
    private const string AuthenticationType = "AuthenticationTypes.Federation";

    public static LambdaClaimsOptions Default { get; } = new();

    public string NameClaimType { get; init; } = "name";

    public string RoleClaimType { get; init; } = "role";

    public ClaimsPrincipal CreatePrincipal(IDictionary<string, string>? claims)
    {
        if ((claims is null) || (claims.Count == 0))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var issuer = claims.TryGetValue("iss", out var iss) ? iss : ClaimsIdentity.DefaultIssuer;
        var identity = new ClaimsIdentity(AuthenticationType, NameClaimType, RoleClaimType);
        foreach (var (type, value) in claims)
        {
#if NET11_0_OR_GREATER
            if (value.StartsWith('[', StringComparison.Ordinal) && value.EndsWith(']', StringComparison.Ordinal))
#else
            if (value.StartsWith('[') && value.EndsWith(']'))
#endif
            {
                foreach (var element in value[1..^1].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    identity.AddClaim(new Claim(type, element, ClaimValueTypes.String, issuer));
                }
            }
            else
            {
                identity.AddClaim(new Claim(type, value, ClaimValueTypes.String, issuer));
            }
        }

        return new ClaimsPrincipal(identity);
    }
}
