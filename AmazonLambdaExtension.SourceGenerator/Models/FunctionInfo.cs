#nullable disable
namespace AmazonLambdaExtension.SourceGenerator.Models;

public class FunctionInfo
{
    public TypeInfo Function { get; set; }

    public List<TypeInfo> ConstructorParameters { get; set; }

    public TypeInfo ServiceLocator { get; set; }

    public string FindService(TypeInfo type)
    {
        return $"GetService<{type.FullName}>()";
    }

    public string FindSerializer()
    {
        return "ResolveSerializer()";
    }
}
