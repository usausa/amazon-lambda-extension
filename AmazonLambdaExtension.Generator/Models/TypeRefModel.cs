namespace AmazonLambdaExtension.Generator.Models;

internal sealed record TypeRefModel(
    string FullName,
    bool IsNullable,
    TypeRefModel? UnderlyingType,
    bool IsArray,
    TypeRefModel? ElementType);
