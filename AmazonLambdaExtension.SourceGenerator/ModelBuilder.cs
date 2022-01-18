namespace AmazonLambdaExtension.SourceGenerator;

using AmazonLambdaExtension.SourceGenerator.Models;

using Microsoft.CodeAnalysis;

public static class ModelBuilder
{
    private const string ServiceResolverAttribute = "AmazonLambdaExtension.Annotations.ServiceResolverAttribute";

    private const string FromQueryAttribute = "AmazonLambdaExtension.Annotations.FromQueryAttribute";
    private const string FromBodyAttribute = "AmazonLambdaExtension.Annotations.FromBodyAttribute";
    private const string FromRouteAttribute = "AmazonLambdaExtension.Annotations.FromRouteAttribute";
    private const string FromHeaderAttribute = "AmazonLambdaExtension.Annotations.FromHeaderAttribute";
    private const string FromServiceAttribute = "AmazonLambdaExtension.Annotations.FromServiceAttribute";

    public static FunctionModel BuildFunctionInfo(ITypeSymbol symbol)
    {
        var ctor = symbol.GetConstructors()
            .OrderByDescending(x => x.Parameters.Length)
            .First();
        var serviceLocator = symbol.GetAttributes()
            .Where(x => x.AttributeClass!.ToDisplayString() == ServiceResolverAttribute)
            .Select(x => (ITypeSymbol)x.ConstructorArguments[0].Value!)
            .FirstOrDefault();

        return new FunctionModel(
            BuildTypeInfo(symbol),
            ctor.Parameters.Select(static x => BuildTypeInfo(x.Type)).ToList(),
            serviceLocator is not null ? BuildTypeInfo(serviceLocator) : null);
    }

    public static HandlerModel BuildHandlerInfo(IMethodSymbol symbol)
    {
        return new HandlerModel(
            symbol.ContainingNamespace.ToDisplayString(),
            $"{symbol.ContainingType.Name}_{symbol.Name}",
            symbol.Name,
            symbol.IsAsync,
            symbol.Parameters.Select(static x => BuildParameterInfo(x)).ToList(),
            ResolveReturnType(symbol));
    }

    private static TypeModel? ResolveReturnType(IMethodSymbol symbol)
    {
        if (symbol.ReturnsVoid)
        {
            return null;
        }

        var fullName = symbol.ToDisplayString();
        if (fullName.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal) ||
            fullName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
        {
            return symbol.ReturnType.IsGenericType() ? BuildTypeInfo(symbol.TypeArguments.First()) : null;
        }

        return BuildTypeInfo(symbol.ReturnType);
    }

    private static ParameterModel BuildParameterInfo(IParameterSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var name = attribute.AttributeClass!.ToDisplayString();
            if (name == FromQueryAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery, FindKeyNameFromAttribute(attribute));
            }
            if (name == FromBodyAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromBody);
            }
            if (name == FromRouteAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromRoute, FindKeyNameFromAttribute(attribute));
            }
            if (name == FromHeaderAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromHeader, FindKeyNameFromAttribute(attribute));
            }
            if (name == FromServiceAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromService);
            }
        }

        return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery);
    }

    private static string? FindKeyNameFromAttribute(AttributeData attributeData)
    {
        foreach (var pair in attributeData.NamedArguments)
        {
            if ((pair.Key == "Name") && (pair.Value.Value is string value))
            {
                return value;
            }
        }

        return null;
    }

    private static TypeModel BuildTypeInfo(ITypeSymbol symbol)
    {
        if (symbol.IsArrayType())
        {
            return new TypeModel(symbol.ToDisplayString(), true, true, BuildTypeInfo(symbol.GetArrayElementType()));
        }

        return new TypeModel(symbol.ToDisplayString(), symbol.IsReferenceType || symbol.IsNullableType(), false, null);
    }
}
