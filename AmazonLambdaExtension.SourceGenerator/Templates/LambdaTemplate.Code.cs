namespace AmazonLambdaExtension.SourceGenerator.Templates;

using AmazonLambdaExtension.SourceGenerator.Models;

public partial class LambdaTemplate
{
    private readonly LambdaModel function;

    private readonly MethodModel method;

    public LambdaTemplate(LambdaModel function, MethodModel method)
    {
        this.function = function;
        this.method = method;
    }
}
