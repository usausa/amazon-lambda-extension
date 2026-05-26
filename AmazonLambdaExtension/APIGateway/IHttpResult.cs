namespace AmazonLambdaExtension.APIGateway;

using System.IO;
using System.Net;

using Amazon.Lambda.Core;

public sealed class HttpResultSerializationOptions
{
    public ILambdaSerializer Serializer { get; set; } = default!;
}

public interface IHttpResult
{
    HttpStatusCode StatusCode { get; }

    Stream Serialize(HttpResultSerializationOptions options);

    IHttpResult AddHeader(string name, string value);
}
