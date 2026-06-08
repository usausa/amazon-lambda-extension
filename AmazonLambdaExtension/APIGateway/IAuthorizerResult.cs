namespace AmazonLambdaExtension.APIGateway;

using Amazon.Lambda.APIGatewayEvents;

public interface IAuthorizerResult
{
    APIGatewayCustomAuthorizerV2SimpleResponse ToSimpleResponse();

    APIGatewayCustomAuthorizerV2IamResponse ToIamResponse(string? routeArn);
}
