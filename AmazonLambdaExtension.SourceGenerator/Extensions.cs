namespace AmazonLambdaExtension.SourceGenerator;

using Microsoft.CodeAnalysis;

public static class Extensions
{
    public static bool IsNullable(this ITypeSymbol symbol)
    {
        return symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }
}
