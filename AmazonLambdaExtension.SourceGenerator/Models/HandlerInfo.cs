namespace AmazonLambdaExtension.SourceGenerator.Models;

public class HandlerInfo
{
    public string Namespace { get; }

    public string WrapperClass { get; }

    public string MethodName { get; }

    public bool IsAsync { get; }

    public List<ParameterInfo> Parameters { get; }

    public TypeInfo? ResultType { get; }

    public HandlerInfo(string @namespace, string wrapperClass, string methodName, bool isAsync, List<ParameterInfo> parameters, TypeInfo? resultType)
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
    public static bool IsSerializerRequired(this HandlerInfo handler) =>
        (handler.ResultType is not null) || handler.Parameters.Any(x => x.ParameterType == ParameterType.FromBody);
}
