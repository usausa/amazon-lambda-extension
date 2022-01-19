namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function1_TestQuery
    {
        private readonly AmazonLambdaExtension.TargetProject.ApplicationServiceResolver serviceResolver;

        private readonly AmazonLambdaExtension.TargetProject.ApplicationFilter filter;

        private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

        private readonly AmazonLambdaExtension.TargetProject.Function1 function;

        public Function1_TestQuery()
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
                if (!AmazonLambdaExtension.Helpers.BindHelper.TryBind<int>(request.QueryStringParameters, "a", out var p0))
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }

                if (!AmazonLambdaExtension.Helpers.BindHelper.TryBindArray<int>(request.MultiValueQueryStringParameters, "b", out var p1))
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
                }

                var output = function.TestQuery(p0, p1);

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
