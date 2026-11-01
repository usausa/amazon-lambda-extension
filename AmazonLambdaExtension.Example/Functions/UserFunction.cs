namespace AmazonLambdaExtension.Example.Functions;

using System.Security.Claims;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.APIGateway;
using AmazonLambdaExtension.Example.Models;

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public partial class UserFunction
{
    private static readonly Action<ILogger, bool, Exception?> MeRequest =
        LoggerMessage.Define<bool>(LogLevel.Information, new EventId(1, nameof(Me)), "Me request. authenticated=[{Authenticated}]");

    private readonly ILogger<UserFunction> log;

    public UserFunction(ILogger<UserFunction> log)
    {
        this.log = log;
    }

    [HttpApi(LambdaHttpMethod.Get, "/me")]
    public IHttpResult Me(ClaimsPrincipal user, [FromCookie("theme")] string? theme)
    {
        var authenticated = user.Identity?.IsAuthenticated == true;
        MeRequest(log, authenticated, null);
        return authenticated
            ? HttpResults.Ok(new MeResponse { Id = user.FindFirst("sub")?.Value, Admin = user.IsInRole("admin"), Theme = theme })
            : HttpResults.Unauthorized();
    }
}
