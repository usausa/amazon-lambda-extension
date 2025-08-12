namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRouteAttribute : Attribute
{
    public string? Name { get; }

    public FromRouteAttribute()
    {
    }

    public FromRouteAttribute(string name)
    {
        Name = name;
    }
}
