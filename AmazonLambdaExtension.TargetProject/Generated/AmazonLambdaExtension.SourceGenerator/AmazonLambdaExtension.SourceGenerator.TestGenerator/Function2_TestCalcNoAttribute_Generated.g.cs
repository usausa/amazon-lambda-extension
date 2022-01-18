namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function2_TestCalcNoAttribute_Generated
    {
        private readonly AmazonLambdaExtension.TargetProject.ServiceLocator serviceLocator;

        private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

        private readonly AmazonLambdaExtension.TargetProject.Function2 function;

        public Function2_TestCalcNoAttribute_Generated()
        {
            serviceLocator = new AmazonLambdaExtension.TargetProject.ServiceLocator();
            serializer = serviceLocator.GetService<AmazonLambdaExtension.Serialization.IBodySerializer>() ?? AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
            function = new AmazonLambdaExtension.TargetProject.Function2(serviceLocator.GetService<AmazonLambdaExtension.TargetProject.ICalculator>());
        }

        public Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            if (request.Headers?.ContainsKey("X-Lambda-Ping") ?? false)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 200 };
            }

            try
            {
                if (!AmazonLambdaExtension.Helpers.BindHelper.TryBind<int>(request.QueryStringParameters, "x", out var p0))
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }

                if (!AmazonLambdaExtension.Helpers.BindHelper.TryBind<int>(request.QueryStringParameters, "y", out var p1))
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }


                var output = function.TestCalcNoAttribute(p0, p1);

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
