namespace AmazonLambdaExtension.SourceGenerator.Models;

public sealed class FilterExecutingModel
{
    public bool IsAsync { get; }

    public bool HasContext { get; }

    public bool HasResult { get; }

    public FilterExecutingModel(bool isAsync, bool hasContext, bool hasResult)
    {
        IsAsync = isAsync;
        HasContext = hasContext;
        HasResult = hasResult;
    }
}
public sealed class FilterExecutedModel
{
    public bool IsAsync { get; }

    public bool HasContext { get; }

    public FilterExecutedModel(bool isAsync, bool hasContext)
    {
        IsAsync = isAsync;
        HasContext = hasContext;
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
