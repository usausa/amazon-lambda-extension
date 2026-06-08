namespace AmazonLambdaExtension.Generator.Models;

using SourceGenerateHelper;

internal enum HandlerType
{
    Event,
    HttpApi,
    FunctionUrl,
    HttpApiAuthorizer
}

internal sealed record HandlerModel(
    string MethodName,
    HandlerType Type,
    bool IsAsync,
    TypeRefModel? ResultType,
    EquatableArray<ParameterModel> Parameters,
    bool ReturnsHttpResult,
    bool ReturnsProxyResponse,
    bool EnableSimpleResponses);
