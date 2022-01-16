namespace WorkTemplate.Models;

using System.Diagnostics.CodeAnalysis;

public class LambdaModel
{
    // lambdaMethodSymbol.ContainingNamespace.ToDisplayString() classのにするか
    [AllowNull]
    public string ContainingNamespace { get; set; }

    [AllowNull]
    public List<TypeModel> ConstructorParameters { get; set; }

    public TypeModel? ServiceLocator { get; set; }

    [AllowNull]
    public TypeModel Function { get; set; }

    public string FindService(TypeModel type)
    {
        return $"GetService<{type.FullName}>()";
    }

    public string FindSerializer()
    {
        return "ResolveSerializer()";
    }
}
