namespace AmazonLambdaExtension.SourceGenerator;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

public static class Extensions
{
    public static bool IsNullable(this ITypeSymbol symbol) =>
        symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    public static IEnumerable<IMethodSymbol> GetConstructors(this ITypeSymbol symbol) =>
        symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Constructor);

    public static bool IsGenericType(this ITypeSymbol symbol) =>
        (symbol as INamedTypeSymbol)?.IsGenericType ?? false;
}
