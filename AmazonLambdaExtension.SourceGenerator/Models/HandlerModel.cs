namespace AmazonLambdaExtension.SourceGenerator.Models;

public sealed class HandlerModel
{
    public string Namespace { get; }

    public string WrapperClass { get; }

    public string MethodName { get; }

    public bool IsAsync { get; }

    public List<ParameterModel> Parameters { get; }

    public TypeModel? ResultType { get; }

    public HandlerModel(string @namespace, string wrapperClass, string methodName, bool isAsync, List<ParameterModel> parameters, TypeModel? resultType)
    {
        Namespace = @namespace;
        WrapperClass = wrapperClass;
        MethodName = methodName;
        IsAsync = isAsync;
        Parameters = parameters;
        ResultType = resultType;
    }
}

public static class HandlerInfoExtensions
{
    public static bool IsSerializerRequired(this HandlerModel handler) =>
        (handler.ResultType is not null) || handler.Parameters.Any(x => x.ParameterType == ParameterType.FromBody);
}
