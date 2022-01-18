namespace AmazonLambdaExtension.SourceGenerator.Models;

using Microsoft.CodeAnalysis;

public class FunctionInfo
{
    public TypeInfo Function { get; }

    public List<TypeInfo> ConstructorParameters { get; }

    public TypeInfo? ServiceLocator { get; }

    public FunctionInfo(TypeInfo function, List<TypeInfo> constructorParameters, TypeInfo? serviceLocator)
    {
        Function = function;
        ConstructorParameters = constructorParameters;
        ServiceLocator = serviceLocator;
    }
}
