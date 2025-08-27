namespace AmazonLambdaExtension.Generator.Models;

// TODO Ex:Refactor
public sealed class FunctionModel
{
    public TypeModel Function { get; }

    public List<TypeModel> ConstructorParameters { get; }

    public FilterModel? Filter { get; }

    public TypeModel? ServiceResolver { get; }

    public bool ResolveFunction { get; }

    public FunctionModel(TypeModel function, List<TypeModel> constructorParameters, FilterModel? filter, TypeModel? serviceResolver, bool resolveFunction)
    {
        Function = function;
        ConstructorParameters = constructorParameters;
        Filter = filter;
        ServiceResolver = serviceResolver;
        ResolveFunction = resolveFunction;
    }
}

public static class FunctionModelExtensions
{
    public static bool IsAsyncRequired(this FunctionModel model) =>
        (model.Filter?.Executing?.IsAsync ?? false) ||
        (model.Filter?.Executed?.IsAsync ?? false);
}
