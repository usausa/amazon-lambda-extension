namespace AmazonLambdaExtension.APIGateway;

using System.Net;

public static class HttpResults
{
    public static IHttpResult Ok(object? body = null) =>
        new HttpResult(HttpStatusCode.OK, body);

    public static IHttpResult Created(string? uri = null, object? body = null)
    {
        var result = new HttpResult(HttpStatusCode.Created, body);
        if (uri is not null)
        {
            result.AddHeader("location", uri);
        }
        return result;
    }

    public static IHttpResult Accepted(object? body = null) =>
        new HttpResult(HttpStatusCode.Accepted, body);

    public static IHttpResult NoContent() =>
        new HttpResult(HttpStatusCode.NoContent);

    public static IHttpResult BadRequest(object? body = null) =>
        new HttpResult(HttpStatusCode.BadRequest, body);

    public static IHttpResult Unauthorized() =>
        new HttpResult(HttpStatusCode.Unauthorized);

    public static IHttpResult Forbid(object? body = null) =>
        new HttpResult(HttpStatusCode.Forbidden, body);

    public static IHttpResult NotFound(object? body = null) =>
        new HttpResult(HttpStatusCode.NotFound, body);

    public static IHttpResult Conflict(object? body = null) =>
        new HttpResult(HttpStatusCode.Conflict, body);

    public static IHttpResult InternalServerError(object? body = null) =>
        new HttpResult(HttpStatusCode.InternalServerError, body);

    public static IHttpResult BadGateway() =>
        new HttpResult(HttpStatusCode.BadGateway);

    public static IHttpResult ServiceUnavailable(int? delaySeconds = null)
    {
        var result = new HttpResult(HttpStatusCode.ServiceUnavailable);
        if (delaySeconds.HasValue)
        {
            result.AddHeader("retry-after", delaySeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return result;
    }

    public static IHttpResult Redirect(string uri, bool permanent = false, bool preserveMethod = false)
    {
        var statusCode = (permanent, preserveMethod) switch
        {
            (true, true) => System.Net.HttpStatusCode.PermanentRedirect,
            (true, false) => System.Net.HttpStatusCode.MovedPermanently,
            (false, true) => System.Net.HttpStatusCode.TemporaryRedirect,
            _ => System.Net.HttpStatusCode.Found,
        };
        var result = new HttpResult(statusCode);
        result.AddHeader("location", uri);
        return result;
    }

    public static IHttpResult NewResult(HttpStatusCode statusCode, object? body = null) =>
        new HttpResult(statusCode, body);
}
