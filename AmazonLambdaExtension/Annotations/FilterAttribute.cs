namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FilterAttribute<TFilter> : Attribute
    where TFilter : AmazonLambdaExtension.Filters.ILambdaFilter
{
    public int Order { get; set; }
}
