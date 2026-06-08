namespace AmazonLambdaExtension.Filters;

using System.Threading.Tasks;

public interface ILambdaFilter
{
    ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next);
}
