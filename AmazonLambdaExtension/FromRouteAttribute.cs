namespace AmazonLambdaExtension;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRouteAttribute : Attribute
{
    public string? Name { get; set; }
}
