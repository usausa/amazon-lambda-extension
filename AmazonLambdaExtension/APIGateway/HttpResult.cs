namespace AmazonLambdaExtension.APIGateway;

using System.IO;
using System.Net;
using System.Text;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public sealed class HttpResult : IHttpResult
{
    private readonly object? body;

    private readonly APIGatewayHttpApiV2ProxyResponse response;

    private List<string>? cookies;

    public HttpStatusCode StatusCode => (HttpStatusCode)response.StatusCode;

    internal HttpResult(HttpStatusCode statusCode, object? body = null)
    {
        response = new APIGatewayHttpApiV2ProxyResponse { StatusCode = (int)statusCode };
        this.body = body;
    }

    public HttpResult AddHeader(string name, string value)
    {
        // Set-Cookie is the canonical header that must not be comma-combined (the Expires
        // attribute itself contains a comma). HTTP API v2 carries cookies in a dedicated field.
        if (String.Equals(name, "set-cookie", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Set-Cookie can not be combined by comma. Use AddCookie instead.", nameof(name));
        }

        response.Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (response.Headers.TryGetValue(name, out var existing))
        {
            response.Headers[name] = existing + "," + value;
        }
        else
        {
            response.Headers[name] = value;
        }
        return this;
    }

    public HttpResult AddCookie(string cookie)
    {
        cookies ??= [];
        cookies.Add(cookie);
        return this;
    }

    APIGatewayHttpApiV2ProxyResponse IHttpResult.ToResponse(ILambdaSerializer serializer)
    {
        if (cookies is not null)
        {
            response.Cookies = cookies.ToArray();
        }

        if (body is not null)
        {
            string contentType;
            switch (body)
            {
                case string s:
                    response.Body = s;
                    contentType = "text/plain";
                    break;
                case Stream stream:
                    using (MemoryStream buffer = new())
                    {
                        stream.CopyTo(buffer);
                        response.Body = Convert.ToBase64String(buffer.GetBuffer(), 0, (int)buffer.Length);
                    }
                    response.IsBase64Encoded = true;
                    contentType = "application/octet-stream";
                    break;
                case byte[] bytes:
                    response.Body = Convert.ToBase64String(bytes);
                    response.IsBase64Encoded = true;
                    contentType = "application/octet-stream";
                    break;
                case IList<byte> byteList:
                    var arr = new byte[byteList.Count];
                    byteList.CopyTo(arr, 0);
                    response.Body = Convert.ToBase64String(arr);
                    response.IsBase64Encoded = true;
                    contentType = "application/octet-stream";
                    break;
                default:
                    using (MemoryStream buffer = new())
                    {
                        serializer.Serialize(body, buffer);
                        response.Body = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
                    }
                    contentType = "application/json";
                    break;
            }

            response.Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            response.Headers.TryAdd("content-type", contentType);
        }

        return response;
    }
}
