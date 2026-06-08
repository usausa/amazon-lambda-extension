namespace AmazonLambdaExtension.Generator.Models;

internal sealed record TypeRefModel(
    string FullName,
    bool IsArray,
    TypeRefModel? ElementType,
    bool IsNullable,
    TypeRefModel? UnderlyingType,
    bool IsReferenceType,
    bool IsNullableReferenceType);

internal static class TypeRefModelExtensions
{
    public static string GetBaseTypeName(this TypeRefModel type)
    {
        if (type.IsNullable && type.UnderlyingType is not null)
        {
            return type.UnderlyingType.FullName;
        }

        if (type.IsArray && type.ElementType is not null)
        {
            return type.ElementType.FullName;
        }

        return type.FullName;
    }
}
