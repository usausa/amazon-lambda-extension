namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromQueryAttribute : Attribute
{
    public string? Name { get; }

    public FromQueryAttribute()
    {
    }

    public FromQueryAttribute(string name)
    {
        Name = name;
    }
}
