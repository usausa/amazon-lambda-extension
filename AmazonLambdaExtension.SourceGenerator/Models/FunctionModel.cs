namespace AmazonLambdaExtension.SourceGenerator.Models;

using Microsoft.CodeAnalysis;

public class FunctionModel
{
    public TypeModel Function { get; }

    public List<TypeModel> ConstructorParameters { get; }

    public TypeModel? ServiceLocator { get; }

    public FunctionModel(TypeModel function, List<TypeModel> constructorParameters, TypeModel? serviceLocator)
    {
        Function = function;
        ConstructorParameters = constructorParameters;
        ServiceLocator = serviceLocator;
    }
}
