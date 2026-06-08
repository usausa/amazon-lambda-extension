namespace AmazonLambdaExtension.APIGateway;

using System.IO;
using System.Net;
using System.Text;

using Amazon.Lambda.APIGatewayEvents;

public sealed class HttpResult : IHttpResult
{
    private readonly object? body;

    private readonly APIGatewayHttpApiV2ProxyResponse response;

    public HttpStatusCode StatusCode => (HttpStatusCode)response.StatusCode;

    internal HttpResult(HttpStatusCode statusCode, object? body = null)
    {
        response = new APIGatewayHttpApiV2ProxyResponse { StatusCode = (int)statusCode };
        this.body = body;
    }

    public HttpResult AddHeader(string name, string value)
    {
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

    APIGatewayHttpApiV2ProxyResponse IHttpResult.ToResponse(HttpResultSerializationOptions options)
    {
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
                        options.Serializer.Serialize(body, buffer);
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
