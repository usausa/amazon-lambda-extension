namespace AmazonLambdaExtension.Generator.Models;

using SourceGenerateHelper;

internal sealed record LambdaModel(
    string Namespace,
    string ClassName,
    TypeRefModel FunctionType,
    EquatableArray<TypeRefModel> ConstructorParameters,
    TypeRefModel? ServiceResolver,
    EquatableArray<TypeRefModel> Filters,
    EquatableArray<HandlerModel> Handlers);
