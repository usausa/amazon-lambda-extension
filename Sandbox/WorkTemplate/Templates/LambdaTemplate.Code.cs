namespace WorkTemplate.Templates;

using WorkTemplate.Models;

public partial class LambdaTemplate
{
    private readonly LambdaModel lambda;

    private readonly MethodModel method;

    public LambdaTemplate(LambdaModel lambda, MethodModel method)
    {
        this.lambda = lambda;
        this.method = method;
    }
}
