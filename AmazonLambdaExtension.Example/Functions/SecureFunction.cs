namespace AmazonLambdaExtension.Example.Functions;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;
using AmazonLambdaExtension.Example.Filters;
using AmazonLambdaExtension.Example.Models;

#pragma warning disable CA1822
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
[Filter<LoggingFilter>(Order = 0)]
[Filter<ApiKeyFilter>(Order = 10)]
public partial class SecureFunction
{
    [HttpApi(LambdaHttpMethod.Get, "/secure/items/{id}")]
    public ValueTask<HttpResult> GetItem([FromRoute] string id) =>
        ValueTask.FromResult(HttpResults.Ok(new ItemResponse { Id = id }));
}
#pragma warning restore CA1822
