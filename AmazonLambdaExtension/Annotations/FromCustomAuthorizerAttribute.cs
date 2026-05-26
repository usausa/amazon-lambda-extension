namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromCustomAuthorizerAttribute : Attribute
{
    public string Name { get; }

    public FromCustomAuthorizerAttribute(string name)
    {
        Name = name;
    }
}
