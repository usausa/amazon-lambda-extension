namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function1_TestRaw
    {
        private readonly AmazonLambdaExtension.TargetProject.ApplicationServiceResolver serviceResolver;

        private readonly AmazonLambdaExtension.TargetProject.ApplicationFilter filter;

        private readonly AmazonLambdaExtension.TargetProject.Function1 function;

        public Function1_TestRaw()
        {
            serviceResolver = new AmazonLambdaExtension.TargetProject.ApplicationServiceResolver();
            filter = new AmazonLambdaExtension.TargetProject.ApplicationFilter();
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
                var p0 = request;

                var p1 = context;

                function.TestRaw(p0, p1);

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
            finally
            {
                await filter.OnFunctionExecuted();
            }
        }
    }
}
