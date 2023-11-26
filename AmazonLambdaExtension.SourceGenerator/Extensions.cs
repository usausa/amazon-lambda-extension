namespace AmazonLambdaExtension.SourceGenerator;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

public static class Extensions
{
    public static bool IsArrayType(this ITypeSymbol symbol) =>
        symbol.TypeKind == TypeKind.Array;

    public static bool IsNullableType(this ITypeSymbol symbol) =>
        symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ITypeSymbol symbol) =>
        (symbol as INamedTypeSymbol)?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty;

    public static ITypeSymbol GetArrayElementType(this ITypeSymbol symbol) =>
        ((IArrayTypeSymbol)symbol).ElementType;

    public static IEnumerable<IMethodSymbol> GetConstructors(this ITypeSymbol symbol) =>
        symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(static x => x.MethodKind == MethodKind.Constructor);
}
