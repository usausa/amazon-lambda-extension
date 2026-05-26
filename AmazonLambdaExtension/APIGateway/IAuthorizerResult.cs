namespace AmazonLambdaExtension.APIGateway;

using System.IO;

public sealed class AuthorizerResultSerializationOptions
{
    public enum AuthorizerFormat
    {
        HttpApiSimple,
        HttpApiIamPolicy
    }

    public AuthorizerFormat Format { get; set; }

    public string? RouteArn { get; set; }
}

public interface IAuthorizerResult
{
    bool IsAuthorized { get; }

    string? PrincipalId { get; }

    IDictionary<string, object>? Context { get; }

    IAuthorizerResult WithContext(string key, object value);

    IAuthorizerResult WithPrincipalId(string principalId);

    Stream Serialize(AuthorizerResultSerializationOptions options);
}
