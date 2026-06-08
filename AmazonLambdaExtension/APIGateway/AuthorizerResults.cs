namespace AmazonLambdaExtension.APIGateway;

public static class AuthorizerResults
{
    public static AuthorizerResult Allow() => new(true);

    public static AuthorizerResult Deny() => new(false);
}
