namespace AmazonLambdaExtension.Generator.Templates;

using AmazonLambdaExtension.Generator.Models;

public partial class EventTemplate
{
    private readonly FunctionModel function;

    private readonly HandlerModel handler;

    public EventTemplate(FunctionModel function, HandlerModel handler)
    {
        this.function = function;
        this.handler = handler;
    }
}
