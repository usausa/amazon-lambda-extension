namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function3_TestTask
    {


        private readonly AmazonLambdaExtension.TargetProject.Function3 function;

        public Function3_TestTask()
        {
            function = new AmazonLambdaExtension.TargetProject.Function3();
        }

        public async System.Threading.Tasks.Task<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            try
            {
                await function.TestTask();

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
