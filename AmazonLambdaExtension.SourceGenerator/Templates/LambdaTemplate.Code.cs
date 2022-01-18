namespace AmazonLambdaExtension.SourceGenerator.Templates;

using AmazonLambdaExtension.SourceGenerator.Models;

public partial class LambdaTemplate
{
    private readonly FunctionModel function;

    private readonly HandlerModel handler;

    public LambdaTemplate(FunctionModel function, HandlerModel handler)
    {
        this.function = function;
        this.handler = handler;
    }
}
