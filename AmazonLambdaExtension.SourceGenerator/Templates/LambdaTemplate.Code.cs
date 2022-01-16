namespace AmazonLambdaExtension.SourceGenerator.Templates;

using AmazonLambdaExtension.SourceGenerator.Models;

public partial class LambdaTemplate
{
    private readonly FunctionInfo function;

    private readonly HandlerInfo handler;

    public LambdaTemplate(FunctionInfo function, HandlerInfo handler)
    {
        this.function = function;
        this.handler = handler;
    }
}
