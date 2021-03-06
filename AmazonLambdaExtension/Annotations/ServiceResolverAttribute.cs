namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceResolverAttribute : Attribute
{
    public Type Type { get; }

    public bool ResolveFunction { get; set; }

    public ServiceResolverAttribute(Type type)
    {
        Type = type;
    }
}
