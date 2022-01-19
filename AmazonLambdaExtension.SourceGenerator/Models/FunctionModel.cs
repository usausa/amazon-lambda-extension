namespace AmazonLambdaExtension.SourceGenerator.Models;

public sealed class FunctionModel
{
    public TypeModel Function { get; }

    public List<TypeModel> ConstructorParameters { get; }

    public TypeModel? ServiceResolver { get; }

    public FilterModel? Filter { get; }

    public FunctionModel(TypeModel function, List<TypeModel> constructorParameters, TypeModel? serviceResolver, FilterModel? filter)
    {
        Function = function;
        ConstructorParameters = constructorParameters;
        ServiceResolver = serviceResolver;
        Filter = filter;
    }
}

public static class FunctionModelExtensions
{
    public static bool IsAsyncRequired(this FunctionModel model) =>
        (model.Filter?.Executing?.IsAsync ?? false) ||
        (model.Filter?.Executed?.IsAsync ?? false);
}
