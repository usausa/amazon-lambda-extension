namespace AmazonLambdaExtension.Filters;

using System.Threading.Tasks;

public interface ILambdaFilter
{
#pragma warning disable CA1716
    ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next);
#pragma warning restore CA1716
}
