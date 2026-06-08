namespace AmazonLambdaExtension.APIGateway;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public sealed class HttpResultSerializationOptions
{
    public ILambdaSerializer Serializer { get; set; } = default!;
}

public interface IHttpResult
{
    APIGatewayHttpApiV2ProxyResponse ToResponse(HttpResultSerializationOptions options);
}
