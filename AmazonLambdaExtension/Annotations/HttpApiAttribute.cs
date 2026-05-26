namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpApiAttribute : Attribute
{
    public LambdaHttpMethod Method { get; }

    public string Template { get; }

    public string? Authorizer { get; set; }

    public HttpApiAttribute(LambdaHttpMethod method, string template)
    {
        Method = method;
        Template = template;
    }
}
