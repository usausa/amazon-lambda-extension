namespace AmazonLambdaExtension.SourceGenerator.Templates;

using AmazonLambdaExtension.SourceGenerator.Models;

public partial class HttpApiTemplate
{
    private readonly FunctionModel function;

    private readonly HandlerModel handler;

    public HttpApiTemplate(FunctionModel function, HandlerModel handler)
    {
        this.function = function;
        this.handler = handler;
    }
}
