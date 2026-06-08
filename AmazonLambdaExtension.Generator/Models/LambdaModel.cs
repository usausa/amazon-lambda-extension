namespace AmazonLambdaExtension.Generator.Models;

using SourceGenerateHelper;

internal sealed record LambdaModel(
    string Namespace,
    string ClassName,
    TypeRefModel FunctionType,
    TypeRefModel? ServiceResolver,
    EquatableArray<TypeRefModel> ConstructorParameters,
    EquatableArray<TypeRefModel> Filters,
    EquatableArray<HandlerModel> Handlers);
