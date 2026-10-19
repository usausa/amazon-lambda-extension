namespace AmazonLambdaExtension.APIGateway;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.Serialization;

public interface IHttpResult
{
    APIGatewayHttpApiV2ProxyResponse ToResponse(IBodySerializer serializer);
}
