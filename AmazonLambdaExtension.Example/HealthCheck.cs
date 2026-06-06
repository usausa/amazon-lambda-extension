namespace AmazonLambdaExtension.Example;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

#pragma warning disable CA1822
[Lambda]
public partial class HealthCheck
{
    [FunctionUrl(
        AuthType = FunctionUrlAuthType.None,
        AllowOrigins = ["*"],
        AllowMethods = ["GET"],
        AllowHeaders = ["Content-Type"],
        MaxAge = 300)]
    public IHttpResult Ping()
        => HttpResults.Ok(new { status = "ok", timestamp = DateTime.UtcNow });
}
#pragma warning restore CA1822
