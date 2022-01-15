namespace AmazonLambdaExtension.SourceGenerator.Templates;

using AmazonLambdaExtension.SourceGenerator.Models;

public partial class FunctionTemplate
{
    private readonly FunctionModel function;

    private readonly MethodModel method;

    public FunctionTemplate(FunctionModel function, MethodModel method)
    {
        this.function = function;
        this.method = method;
    }
}
