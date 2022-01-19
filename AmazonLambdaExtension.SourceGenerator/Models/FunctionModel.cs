namespace AmazonLambdaExtension.SourceGenerator.Models;

public class FunctionModel
{
    public TypeModel Function { get; }

    public List<TypeModel> ConstructorParameters { get; }

    public TypeModel? ServiceResolver { get; }

    public FunctionModel(TypeModel function, List<TypeModel> constructorParameters, TypeModel? serviceResolver)
    {
        Function = function;
        ConstructorParameters = constructorParameters;
        ServiceResolver = serviceResolver;
    }
}
