namespace AmazonLambdaExtension.TargetProject.Tests;

using Xunit;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.TargetProject.Components.Logging;

using Microsoft.Extensions.Logging;

public class FunctionTest
{
    [Fact]
    public void TestGetMethod()
    {
        using var loggerFactory = new LambdaLoggerFactory(LogLevel.Information, null);
        var functions = new Functions(loggerFactory.CreateLogger<Functions>());

        var request = new APIGatewayProxyRequest();
        var response = functions.Get(request);

        Assert.Equal(200, response.StatusCode);
    }
}
