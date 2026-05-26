namespace AmazonLambdaExtension.Example;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class CrudFunctions
{
    private readonly DataService data;

    public CrudFunctions(DataService data)
    {
        this.data = data;
    }

    [HttpApi(LambdaHttpMethod.Get, "/items/{id}")]
    public async ValueTask<IHttpResult> GetItem(
        [FromRoute] string id,
        [FromQuery] int page,
        ILambdaContext context)
    {
        context.Logger.LogInformation($"GetItem id={id} page={page}");
        var item = await data.GetAsync(id, page);
        return item is null ? HttpResults.NotFound() : HttpResults.Ok(item);
    }

    [HttpApi(LambdaHttpMethod.Get, "/items")]
    public async ValueTask<IHttpResult> ListItems(
        [FromQuery] int[] ids,
        [FromHeader("x-tenant-id")] string tenantId)
    {
        var items = await data.ListAsync(tenantId, ids);
        return HttpResults.Ok(items);
    }

    [HttpApi(LambdaHttpMethod.Post, "/items", Authorizer = nameof(Authorize))]
    public async ValueTask<IHttpResult> CreateItem(
        [FromBody] CreateItemInput input,
        [FromCustomAuthorizer("role")] string role)
    {
        if (role != "admin")
        {
            return HttpResults.Forbid();
        }

        var created = await data.CreateAsync(input);
        return HttpResults.Created($"/items/{created.Id}", created);
    }

    [HttpApiAuthorizer(EnableSimpleResponses = true)]
    public async ValueTask<IAuthorizerResult> Authorize(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        context.Logger.LogInformation("Authorize called");
        if (!request.Headers.TryGetValue("authorization", out var token) ||
            !await data.IsValidTokenAsync(token))
        {
            return AuthorizerResults.Deny();
        }

        return AuthorizerResults.Allow()
            .WithPrincipalId("user-123")
            .WithContext("role", "admin");
    }
}
