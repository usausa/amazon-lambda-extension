namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function1_Get2_Generated
    {
        private readonly AmazonLambdaExtension.TargetProject.ServiceLocator serviceLocator;

        private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

        private readonly AmazonLambdaExtension.TargetProject.Function1 function;

        public Function1_Get2_Generated()
        {
            serviceLocator = new AmazonLambdaExtension.TargetProject.ServiceLocator();
            serializer = serviceLocator.GetService<AmazonLambdaExtension.Serialization.IBodySerializer>() ?? AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
            function = new AmazonLambdaExtension.TargetProject.Function1(serviceLocator.GetService<Microsoft.Extensions.Logging.ILogger<AmazonLambdaExtension.TargetProject.Function1>>());
        }

        public Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            if (request.Headers?.ContainsKey("X-Lambda-Hot-Load") ?? false)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 200 };
            }

            try
            {
                Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest p0;
                try
                {
                    p0 = serializer.Deserialize<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest>(request.Body);
                }
                catch (System.Exception ex)
                {
                    context.Logger.LogLine(ex.ToString());
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }

                // TODO validation


                var output = function.Get2(p0);
                if (output == null)
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 404 };
                }

                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse
                {
                    Body = serializer.Serialize(output),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    StatusCode = 200
                };
            }
            catch (AmazonLambdaExtension.ApiException ex)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = ex.StatusCode };
            }
            catch (System.Exception ex)
            {
                context.Logger.LogLine(ex.ToString());
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 500 };
            }
        }
    }
}
