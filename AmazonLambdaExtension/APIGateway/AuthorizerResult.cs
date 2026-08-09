namespace AmazonLambdaExtension.APIGateway;

using Amazon.Lambda.APIGatewayEvents;

public sealed class AuthorizerResult : IAuthorizerResult
{
    private Dictionary<string, object>? context;

    private string? principalId;

    public bool IsAuthorized { get; }

    internal AuthorizerResult(bool isAuthorized)
    {
        IsAuthorized = isAuthorized;
    }

    // The authorizer context accepts only string, number and boolean values; anything else
    // fails later at serialization time, so the accepted types are limited up front.
    public AuthorizerResult WithContext(string key, string value) => WithContextCore(key, value);

    public AuthorizerResult WithContext(string key, bool value) => WithContextCore(key, value);

    public AuthorizerResult WithContext(string key, int value) => WithContextCore(key, value);

    public AuthorizerResult WithContext(string key, long value) => WithContextCore(key, value);

    public AuthorizerResult WithContext(string key, double value) => WithContextCore(key, value);

    private AuthorizerResult WithContextCore(string key, object value)
    {
        context ??= [];
        context[key] = value;
        return this;
    }

    public AuthorizerResult WithPrincipalId(string value)
    {
        principalId = value;
        return this;
    }

    APIGatewayCustomAuthorizerV2SimpleResponse IAuthorizerResult.ToSimpleResponse()
    {
        return new APIGatewayCustomAuthorizerV2SimpleResponse
        {
            IsAuthorized = IsAuthorized,
            Context = context
        };
    }

    APIGatewayCustomAuthorizerV2IamResponse IAuthorizerResult.ToIamResponse(string? routeArn)
    {
        return new APIGatewayCustomAuthorizerV2IamResponse
        {
            PrincipalID = principalId ?? "user",
            PolicyDocument = new APIGatewayCustomAuthorizerPolicy
            {
                Version = "2012-10-17",
                Statement =
                [
                    new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement
                    {
                        Effect = IsAuthorized ? "Allow" : "Deny",
                        Action = ["execute-api:Invoke"],
                        Resource = [routeArn ?? "*"]
                    }
                ]
            },
            Context = context
        };
    }
}
