namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function1_TestBody
    {
        private readonly AmazonLambdaExtension.TargetProject.ApplicationServiceResolver serviceResolver;

        private readonly AmazonLambdaExtension.TargetProject.ApplicationFilter filter;

        private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

        private readonly AmazonLambdaExtension.TargetProject.Function1 function;

        public Function1_TestBody()
        {
            serviceResolver = new AmazonLambdaExtension.TargetProject.ApplicationServiceResolver();
            filter = new AmazonLambdaExtension.TargetProject.ApplicationFilter();
            serializer = serviceResolver.GetService<AmazonLambdaExtension.Serialization.IBodySerializer>() ?? AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
            function = new AmazonLambdaExtension.TargetProject.Function1(serviceResolver.GetService<Microsoft.Extensions.Logging.ILogger<AmazonLambdaExtension.TargetProject.Function1>>());
        }

        public async System.Threading.Tasks.Task<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            var executingResult = await filter.OnFunctionExecuting(context);
            if (executingResult != null)
            {
                return executingResult;
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

                var output = function.TestBody(p0);
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
            finally
            {
                await filter.OnFunctionExecuted();
            }
        }
    }
}
