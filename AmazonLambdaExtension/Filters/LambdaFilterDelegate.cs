namespace AmazonLambdaExtension.Filters;

using System.Threading.Tasks;

#pragma warning disable CA1711
public delegate ValueTask LambdaFilterDelegate(LambdaInvocationContext context);
#pragma warning restore CA1711
