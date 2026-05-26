namespace AmazonLambdaExtension.APIGateway;

public static class AuthorizerResults
{
    public static IAuthorizerResult Allow() => new AuthorizerResult(isAuthorized: true);

    public static IAuthorizerResult Deny() => new AuthorizerResult(isAuthorized: false);
}
