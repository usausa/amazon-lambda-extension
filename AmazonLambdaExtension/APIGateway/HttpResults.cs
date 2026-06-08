namespace AmazonLambdaExtension.APIGateway;

using System.Net;

public static class HttpResults
{
    public static HttpResult Ok(object? body = null) =>
        new(HttpStatusCode.OK, body);

    public static HttpResult Created(string? uri = null, object? body = null)
    {
        var result = new HttpResult(HttpStatusCode.Created, body);
        if (uri is not null)
        {
            result.AddHeader("location", uri);
        }
        return result;
    }

    public static HttpResult Accepted(object? body = null) => new(HttpStatusCode.Accepted, body);

    public static HttpResult NoContent() => new(HttpStatusCode.NoContent);

    public static HttpResult BadRequest(object? body = null) => new(HttpStatusCode.BadRequest, body);

    public static HttpResult Unauthorized() => new(HttpStatusCode.Unauthorized);

    public static HttpResult Forbid(object? body = null) => new(HttpStatusCode.Forbidden, body);

    public static HttpResult NotFound(object? body = null) => new(HttpStatusCode.NotFound, body);

    public static HttpResult Conflict(object? body = null) => new(HttpStatusCode.Conflict, body);

    public static HttpResult InternalServerError(object? body = null) => new(HttpStatusCode.InternalServerError, body);

    public static HttpResult BadGateway() => new(HttpStatusCode.BadGateway);

    public static HttpResult ServiceUnavailable(int? delaySeconds = null)
    {
        var result = new HttpResult(HttpStatusCode.ServiceUnavailable);
        if (delaySeconds.HasValue)
        {
            result.AddHeader("retry-after", delaySeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return result;
    }

    public static HttpResult Redirect(string uri, bool permanent = false, bool preserveMethod = false)
    {
        var statusCode = (permanent, preserveMethod) switch
        {
            (true, true) => HttpStatusCode.PermanentRedirect,
            (true, false) => HttpStatusCode.MovedPermanently,
            (false, true) => HttpStatusCode.TemporaryRedirect,
            _ => HttpStatusCode.Found
        };
        var result = new HttpResult(statusCode);
        result.AddHeader("location", uri);
        return result;
    }

    public static HttpResult NewResult(HttpStatusCode statusCode, object? body = null) => new(statusCode, body);
}
