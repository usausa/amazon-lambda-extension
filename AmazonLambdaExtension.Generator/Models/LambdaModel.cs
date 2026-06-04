namespace AmazonLambdaExtension.Generator.Models;

using SourceGenerateHelper;

internal sealed record LambdaModel(
    string Namespace,
    string ClassName,
    TypeRefModel FunctionType,
    EquatableArray<TypeRefModel> ConstructorParameters,
    ServiceResolverModel? ServiceResolver,
    EquatableArray<FilterDescriptorModel> Filters,
    EquatableArray<HandlerModel> Handlers);

internal sealed record HandlerModel(
    string MethodName,
    HandlerKind Kind,
    bool IsAsync,
    TypeRefModel? ResultType,
    bool ReturnsHttpResult,
    EquatableArray<ParameterModel> Parameters,
    AuthorizerHandlerOptions? Authorizer);

internal enum HandlerKind
{
    Event,
    HttpApi,
    FunctionUrl,
    HttpApiAuthorizer
}

internal sealed record AuthorizerHandlerOptions(
    bool EnableSimpleResponses);

internal sealed record ParameterModel(
    string Name,
    TypeRefModel Type,
    ParameterBindingKind BindingKind,
    string Key,
    string ConverterMethod,
    bool SkipValidation);

internal enum ParameterBindingKind
{
    Request,
    Context,
    FromQuery,
    FromHeader,
    FromRoute,
    FromBody,
    FromServices,
    FromCustomAuthorizer
}

internal sealed record TypeRefModel(
    string FullName,
    bool IsNullable,
    TypeRefModel? UnderlyingType,
    bool IsArray,
    TypeRefModel? ElementType);

internal sealed record FilterDescriptorModel(
    int Index,
    TypeRefModel FilterType,
    int Order);

internal sealed record ServiceResolverModel(
    TypeRefModel Type);
