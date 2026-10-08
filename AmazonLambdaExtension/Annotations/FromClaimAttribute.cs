namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromClaimAttribute : Attribute
{
    public string Name { get; }

    public FromClaimAttribute(string name)
    {
        Name = name;
    }
}
