namespace AmazonLambdaExtension;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromQueryAttribute : Attribute
{
    public string? Name { get; set; }
}
