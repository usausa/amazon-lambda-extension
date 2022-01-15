namespace AmazonLambdaExtension;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromHeaderAttribute : Attribute
{
    public string? Name { get; set; }
}
