namespace AmazonLambdaExtension.Generator.Templates;

using AmazonLambdaExtension.Generator.Models;

public partial class ApiTemplate
{
    private readonly FunctionModel function;

    private readonly HandlerModel handler;

    public ApiTemplate(FunctionModel function, HandlerModel handler)
    {
        this.function = function;
        this.handler = handler;
    }
}
