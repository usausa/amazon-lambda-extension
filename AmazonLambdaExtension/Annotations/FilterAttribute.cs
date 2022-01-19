namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FilterAttribute : Attribute
{
    public Type Type { get; }

    public FilterAttribute(Type type)
    {
        Type = type;
    }
}
