namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromAuthorizerAttribute : Attribute
{
    public string Name { get; }

    public FromAuthorizerAttribute(string name)
    {
        Name = name;
    }
}
