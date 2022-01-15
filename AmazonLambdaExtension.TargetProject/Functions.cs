namespace AmazonLambdaExtension.TargetProject;

using System.Net;

using Amazon.Lambda.APIGatewayEvents;

using Microsoft.Extensions.Logging;

[Function]
[ServiceResolver(typeof(ServiceLocator))]
public class Function1
{
    private readonly ILogger<Function1> logger;

    public Function1(ILogger<Function1> logger)
    {
        this.logger = logger;
    }

    [ApiGateway]
    public APIGatewayProxyResponse Get1(APIGatewayProxyRequest request)
    {
        logger.LogInformation("Get Request. path=[{Path}]", request.Path);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = "Hello AWS",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }

    [ApiGateway]
    public APIGatewayProxyResponse Get2(APIGatewayProxyRequest request)
    {
        logger.LogInformation("Get Request. path=[{Path}]", request.Path);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = "Hello AWS",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }
}

[Function]
[ServiceResolver(typeof(ServiceLocator))]
public class Function2
{
    private readonly ILogger<Function2> logger;

    public Function2(ILogger<Function2> logger)
    {
        this.logger = logger;
    }

    [ApiGateway]
    public APIGatewayProxyResponse Get1(APIGatewayProxyRequest request)
    {
        logger.LogInformation("Get Request. path=[{Path}]", request.Path);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = "Hello AWS",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };

        return response;
    }
}
