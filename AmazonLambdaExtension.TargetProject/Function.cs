namespace AmazonLambdaExtension.TargetProject;

using System.Net;

using Amazon.Lambda.APIGatewayEvents;

using Microsoft.Extensions.Logging;

public class Functions
{
    private readonly ILogger<Functions> logger;

    public Functions(ILogger<Functions> logger)
    {
        this.logger = logger;
    }

    public APIGatewayProxyResponse Get(APIGatewayProxyRequest request)
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
