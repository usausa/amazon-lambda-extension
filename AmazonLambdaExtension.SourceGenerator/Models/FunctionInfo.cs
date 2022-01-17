#nullable disable
namespace AmazonLambdaExtension.SourceGenerator.Models;

using Microsoft.CodeAnalysis;

public class FunctionInfo
{
    private readonly ITypeSymbol serviceLocatorSymbol;

    public TypeInfo Function { get; }

    public List<TypeInfo> ConstructorParameters { get; }

    public TypeInfo ServiceLocator { get; }

    public FunctionInfo(
        TypeInfo function,
        List<TypeInfo> constructorParameters,
        TypeInfo serviceLocator,
        ITypeSymbol serviceLocatorSymbol)
    {
        Function = function;
        ConstructorParameters = constructorParameters;
        ServiceLocator = serviceLocator;
        this.serviceLocatorSymbol = serviceLocatorSymbol;
    }

    // TODO ServiceLocatorInfo ?
    public string FindService(TypeInfo type)
    {
        return $"GetService<{type.FullName}>()";
    }

    public string FindSerializer()
    {
        return "ResolveSerializer()";
    }
}
