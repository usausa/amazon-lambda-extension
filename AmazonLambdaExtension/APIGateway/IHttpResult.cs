namespace AmazonLambdaExtension.APIGateway;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public interface IHttpResult
{
    APIGatewayHttpApiV2ProxyResponse ToResponse(ILambdaSerializer serializer);
}
