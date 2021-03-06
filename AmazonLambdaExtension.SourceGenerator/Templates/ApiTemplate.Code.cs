namespace AmazonLambdaExtension.SourceGenerator.Templates;

using AmazonLambdaExtension.SourceGenerator.Models;

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
