namespace AmazonLambdaExtension.Example;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

#pragma warning disable CA1822
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
[Filter<LoggingFilter>(Order = 0)]
[Filter<ApiKeyFilter>(Order = 10)]
public partial class SecureFunctions
{
    [HttpApi(LambdaHttpMethod.Get, "/secure/items/{id}")]
    public ValueTask<HttpResult> GetItem([FromRoute] string id)
        => ValueTask.FromResult(HttpResults.Ok(new { id }));
}
#pragma warning restore CA1822
