namespace AmazonLambdaExtension.SourceGenerator;

using AmazonLambdaExtension.SourceGenerator.Models;

using Microsoft.CodeAnalysis;

public static class ModelBuilder
{
    private const string ServiceResolverAttribute = "AmazonLambdaExtension.Annotations.ServiceResolverAttribute";
    private const string FilterAttribute = "AmazonLambdaExtension.Annotations.FilterAttribute";

    private const string FromQueryAttribute = "AmazonLambdaExtension.Annotations.FromQueryAttribute";
    private const string FromBodyAttribute = "AmazonLambdaExtension.Annotations.FromBodyAttribute";
    private const string FromRouteAttribute = "AmazonLambdaExtension.Annotations.FromRouteAttribute";
    private const string FromHeaderAttribute = "AmazonLambdaExtension.Annotations.FromHeaderAttribute";
    private const string FromServicesAttribute = "AmazonLambdaExtension.Annotations.FromServicesAttribute";

    private const string AmazonLambdaNamespace = "Amazon.Lambda.";

    private const string FunctionExecuting = "OnFunctionExecuting";
    private const string FunctionExecuted = "OnFunctionExecuted";

    public static FunctionModel BuildFunctionInfo(ITypeSymbol symbol)
    {
        var ctor = symbol.GetConstructors()
            .OrderByDescending(x => x.Parameters.Length)
            .First();
        var serviceResolver = symbol.GetAttributes()
            .Where(x => x.AttributeClass!.ToDisplayString() == ServiceResolverAttribute)
            .Select(x => (ITypeSymbol)x.ConstructorArguments[0].Value!)
            .FirstOrDefault();
        var filter = symbol.GetAttributes()
            .Where(x => x.AttributeClass!.ToDisplayString() == FilterAttribute)
            .Select(x => (ITypeSymbol)x.ConstructorArguments[0].Value!)
            .FirstOrDefault();

        // TODO
        return new FunctionModel(
            BuildTypeInfo(symbol),
            ctor.Parameters.Select(static x => BuildTypeInfo(x.Type)).ToList(),
            filter is not null ? BuildFilterInfo(filter) : null,
            serviceResolver is not null ? BuildTypeInfo(serviceResolver) : null,
            false);
    }

    private static FilterModel BuildFilterInfo(ITypeSymbol symbol)
    {
        FilterExecutingModel? executing = null;
        FilterExecutedModel? executed = null;

        foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.Name == FunctionExecuting)
            {
                var returnType = ResolveReturnType(method);
                executing = new FilterExecutingModel(method.IsAsync, returnType is not null);
            }
            else if (method.Name == FunctionExecuted)
            {
                executed = new FilterExecutedModel(method.IsAsync);
            }
        }

        return new FilterModel(BuildTypeInfo(symbol), executing, executed);
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

        var fullName = symbol.ReturnType.ToDisplayString();
        if (fullName.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal) ||
            fullName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
        {
            var typeArguments = symbol.ReturnType.GetTypeArguments();
            return typeArguments.Length > 0 ? BuildTypeInfo(typeArguments[0]) : null;
        }

        return BuildTypeInfo(symbol.ReturnType);
    }

    private static ParameterModel BuildParameterInfo(IParameterSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass!.ToDisplayString();
            if (attributeName == FromQueryAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery, false, FindKeyNameFromAttribute(attribute));
            }
            if (attributeName == FromBodyAttribute)
            {
                // TODO
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromBody, false);
            }
            if (attributeName == FromRouteAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromRoute, false, FindKeyNameFromAttribute(attribute));
            }
            if (attributeName == FromHeaderAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromHeader, false, FindKeyNameFromAttribute(attribute));
            }
            if (attributeName == FromServicesAttribute)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromServices, false);
            }
        }

        var typeName = symbol.Type.ToDisplayString();
        if (typeName.StartsWith(AmazonLambdaNamespace, StringComparison.Ordinal))
        {
            return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.None, false);
        }

        return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery, false);
    }

    private static string? FindKeyNameFromAttribute(AttributeData attributeData)
    {
        return attributeData.ConstructorArguments.Length > 0 ? attributeData.ConstructorArguments[0].Value?.ToString() : null;
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
