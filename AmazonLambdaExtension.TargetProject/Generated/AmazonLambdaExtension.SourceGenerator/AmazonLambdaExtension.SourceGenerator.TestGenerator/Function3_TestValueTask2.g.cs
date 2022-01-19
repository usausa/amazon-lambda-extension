namespace AmazonLambdaExtension.TargetProject
{
    public sealed class Function3_TestValueTask2
    {


        private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

        private readonly AmazonLambdaExtension.TargetProject.Function3 function;

        public Function3_TestValueTask2()
        {
            serializer = AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
            function = new AmazonLambdaExtension.TargetProject.Function3();
        }

        public async System.Threading.Tasks.Task<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
        {
            try
            {
                var output = await function.TestValueTask2();

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
