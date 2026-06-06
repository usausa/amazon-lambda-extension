namespace AmazonLambdaExtension.Annotations;

// ReSharper disable once UnusedTypeParameter
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FilterAttribute<TFilter> : Attribute
    where TFilter : Filters.ILambdaFilter
{
    public int Order { get; set; }
}
