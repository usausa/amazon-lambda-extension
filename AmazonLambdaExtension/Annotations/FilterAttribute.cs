namespace AmazonLambdaExtension.Annotations;

using AmazonLambdaExtension.Filters;

// ReSharper disable once UnusedTypeParameter
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FilterAttribute<TFilter> : Attribute
    where TFilter : ILambdaFilter
{
    public int Order { get; set; }
}
