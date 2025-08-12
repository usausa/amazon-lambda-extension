namespace AmazonLambdaExtension.Generator.Models;

// TODO record
public sealed class FilterExecutingModel
{
    public bool IsAsync { get; }

    public bool HasResult { get; }

    public FilterExecutingModel(bool isAsync, bool hasResult)
    {
        IsAsync = isAsync;
        HasResult = hasResult;
    }
}

public sealed class FilterExecutedModel
{
    public bool IsAsync { get; }

    public FilterExecutedModel(bool isAsync)
    {
        IsAsync = isAsync;
    }
}

public sealed class FilterModel
{
    public TypeModel Type { get; }

    public FilterExecutingModel? Executing { get; }

    public FilterExecutedModel? Executed { get; }

    public FilterModel(TypeModel type, FilterExecutingModel? executing, FilterExecutedModel? executed)
    {
        Type = type;
        Executing = executing;
        Executed = executed;
    }
}
