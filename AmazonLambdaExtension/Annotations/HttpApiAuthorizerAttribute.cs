namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpApiAuthorizerAttribute : Attribute
{
    public bool EnableSimpleResponses { get; set; } = true;
}
