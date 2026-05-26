namespace AmazonLambdaExtension.APIGateway;

using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

internal sealed class HttpResult : IHttpResult
{
    public HttpStatusCode StatusCode { get; }

    private readonly object? body;
    private Dictionary<string, string>? headers;

    internal HttpResult(HttpStatusCode statusCode, object? body = null)
    {
        StatusCode = statusCode;
        this.body = body;
    }

    public IHttpResult AddHeader(string name, string value)
    {
        headers ??= [];
        if (headers.TryGetValue(name, out var existing))
        {
            headers[name] = existing + "," + value;
        }
        else
        {
            headers[name] = value;
        }
        return this;
    }

    public Stream Serialize(HttpResultSerializationOptions options)
    {
        var bodyStr = BuildBodyString(options, out var isBase64, out var contentType);
        var responseHeaders = BuildHeaders(contentType);

        MemoryStream ms = new();
        using (var writer = new Utf8JsonWriter(ms))
        {
            writer.WriteStartObject();
            writer.WriteNumber("statusCode"u8, (int)StatusCode);
            writer.WriteBoolean("isBase64Encoded"u8, isBase64);
            if (responseHeaders is not null && responseHeaders.Count > 0)
            {
                writer.WriteStartObject("headers"u8);
                foreach (var kv in responseHeaders)
                {
                    writer.WriteString(kv.Key, kv.Value);
                }
                writer.WriteEndObject();
            }
            if (bodyStr is not null)
            {
                writer.WriteString("body"u8, bodyStr);
            }
            else
            {
                writer.WriteNull("body"u8);
            }
            writer.WriteEndObject();
        }
        ms.Position = 0;
        return ms;
    }

    private string? BuildBodyString(HttpResultSerializationOptions options, out bool isBase64, out string? contentType)
    {
        isBase64 = false;
        contentType = null;

        if (body is null)
        {
            return null;
        }

        if (body is string s)
        {
            contentType = "text/plain";
            return s;
        }

        if (body is Stream stream)
        {
            using MemoryStream streamBuffer = new();
            stream.CopyTo(streamBuffer);
            isBase64 = true;
            contentType = "application/octet-stream";
            return Convert.ToBase64String(streamBuffer.ToArray());
        }

        if (body is byte[] bytes)
        {
            isBase64 = true;
            contentType = "application/octet-stream";
            return Convert.ToBase64String(bytes);
        }

        if (body is IList<byte> byteList)
        {
            var arr = new byte[byteList.Count];
            byteList.CopyTo(arr, 0);
            isBase64 = true;
            contentType = "application/octet-stream";
            return Convert.ToBase64String(arr);
        }

        MemoryStream jsonBuffer = new();
        options.Serializer.Serialize(body, jsonBuffer);
        contentType = "application/json";
        return Encoding.UTF8.GetString(jsonBuffer.ToArray());
    }

    private Dictionary<string, string>? BuildHeaders(string? contentType)
    {
        Dictionary<string, string>? responseHeaders = null;
        if (headers is not null)
        {
            responseHeaders = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        }

        if (contentType is not null)
        {
            responseHeaders ??= [];
            responseHeaders.TryAdd("content-type", contentType);
        }

        return responseHeaders;
    }
}
