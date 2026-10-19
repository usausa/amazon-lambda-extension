namespace AmazonLambdaExtension.Example.Functions;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;
using AmazonLambdaExtension.Example.Models;

#pragma warning disable CA1822
[Lambda]
public partial class HealthCheckFunction
{
    [FunctionUrl]
    public IHttpResult Ping() => HttpResults.Ok(new HealthStatus("ok", DateTime.UtcNow));
}
#pragma warning restore CA1822
