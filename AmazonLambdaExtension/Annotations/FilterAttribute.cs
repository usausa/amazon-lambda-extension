namespace AmazonLambdaExtension.Annotations;

using AmazonLambdaExtension.Filters;

// ReSharper disable once UnusedTypeParameter
// On an assembly, the filter applies to every [Lambda] class in it
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FilterAttribute<TFilter> : Attribute
    where TFilter : ILambdaFilter
{
    public int Order { get; set; }
}
