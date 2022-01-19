namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function3_TestVoid
    {


        private readonly AmazonLambdaExtension.TargetProject.Function3 function;

        public Function3_TestVoid()
        {
            function = new AmazonLambdaExtension.TargetProject.Function3();
        }

        public Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            try
            {
                function.TestVoid();

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
