namespace AmazonLambdaExtension.Generator.Models;

using SourceGenerateHelper;

internal enum HandlerType
{
    Event,
    HttpApi,
    FunctionUrl,
    HttpApiAuthorizer
}

internal enum ResponseType
{
    Poco,
    HttpResult,
    ProxyResponse
}

internal sealed record HandlerModel(
    string MethodName,
    HandlerType Type,
    bool IsAsync,
    TypeRefModel? ResultType,
    EquatableArray<ParameterModel> Parameters,
    ResponseType ResponseType,
    bool EnableSimpleResponses);

internal static class HandlerModelExtensions
{
    public static ParameterModel? GetRequestParam(this HandlerModel handler)
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var p in handler.Parameters)
        {
            if (p.BindingType == ParameterBindingType.Request)
            {
                return p;
            }
        }

        return null;
    }
}
