﻿namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function1_TestBodyVoid
    {
        private readonly AmazonLambdaExtension.TargetProject.ServiceLocator serviceLocator;

        private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

        private readonly AmazonLambdaExtension.TargetProject.Function1 function;

        public Function1_TestBodyVoid()
        {
            serviceLocator = new AmazonLambdaExtension.TargetProject.ServiceLocator();
            serializer = serviceLocator.GetService<AmazonLambdaExtension.Serialization.IBodySerializer>() ?? AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
            function = new AmazonLambdaExtension.TargetProject.Function1(serviceLocator.GetService<Microsoft.Extensions.Logging.ILogger<AmazonLambdaExtension.TargetProject.Function1>>());
        }

        public Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            if (request.Headers?.ContainsKey("X-Lambda-Ping") ?? false)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 200 };
            }

            try
            {
                AmazonLambdaExtension.TargetProject.Input p0;
                try
                {
                    p0 = serializer.Deserialize<AmazonLambdaExtension.TargetProject.Input>(request.Body);
                }
                catch (System.Exception ex)
                {
                    context.Logger.LogLine(ex.ToString());
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }

                if (!AmazonLambdaExtension.Helpers.ValidationHelper.Validate(p0))
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }

                function.TestBodyVoid(p0);

                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse
                {
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
