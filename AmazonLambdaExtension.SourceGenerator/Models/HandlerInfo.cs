#nullable disable
namespace AmazonLambdaExtension.SourceGenerator.Models;

public class HandlerInfo
{
    // lambdaMethodSymbol.ContainingNamespace.ToDisplayString() classのにするか
    public string ContainingNamespace { get; set; }

    // $"{lambdaMethodSymbol.ContainingType.Name}_{lambdaMethodSymbol.Name}_Generated";
    public string WrapperClassName { get; set; }

    public bool IsAsync { get; set; }

    public string MethodName { get; set; }

    public List<ParameterInfo> Parameters { get; set; }

    public TypeInfo ResultType { get; set; }
}

public static class HandlerInfoExtensions
{
    public static bool HasBodyParameter(this HandlerInfo handler) =>
        handler.Parameters.Any(x => x.ParameterType == ParameterType.FromBody);
}
