namespace AmazonLambdaExtension.SourceGenerator;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.SourceGenerator.Models;

using Microsoft.CodeAnalysis;

public static class ModelBuilder
{
    private const string ServiceResolverAttributeName = "AmazonLambdaExtension.Annotations.ServiceResolverAttribute";
    private const string FilterAttributeName = "AmazonLambdaExtension.Annotations.FilterAttribute";

    private const string FromQueryAttributeName = "AmazonLambdaExtension.Annotations.FromQueryAttribute";
    private const string FromBodyAttributeName = "AmazonLambdaExtension.Annotations.FromBodyAttribute";
    private const string FromRouteAttributeName = "AmazonLambdaExtension.Annotations.FromRouteAttribute";
    private const string FromHeaderAttributeName = "AmazonLambdaExtension.Annotations.FromHeaderAttribute";
    private const string FromServicesAttributeName = "AmazonLambdaExtension.Annotations.FromServicesAttribute";

    private const string AmazonLambdaNamespace = "Amazon.Lambda.";

    private const string FunctionExecuting = "OnFunctionExecuting";
    private const string FunctionExecuted = "OnFunctionExecuted";

    public static FunctionModel BuildFunctionInfo(ITypeSymbol symbol)
    {
        var ctor = symbol.GetConstructors()
            .OrderByDescending(static x => x.Parameters.Length)
            .First();
        var serviceResolver = symbol.GetAttributes()
            .FirstOrDefault(static x => x.AttributeClass!.ToDisplayString() == ServiceResolverAttributeName);
        var filter = symbol.GetAttributes()
            .FirstOrDefault(static x => x.AttributeClass!.ToDisplayString() == FilterAttributeName);

        return new FunctionModel(
            BuildTypeInfo(symbol),
            ctor.Parameters.Select(static x => BuildTypeInfo(x.Type)).ToList(),
            filter is not null ? BuildFilterInfo((ITypeSymbol)filter.ConstructorArguments[0].Value!) : null,
            serviceResolver is not null ? BuildTypeInfo((ITypeSymbol)serviceResolver.ConstructorArguments[0].Value!) : null,
            serviceResolver is not null && ResolveNamedArgument(serviceResolver, nameof(ServiceResolverAttribute.ResolveFunction), false));
    }

    private static FilterModel BuildFilterInfo(ITypeSymbol symbol)
    {
        FilterExecutingModel? executing = null;
        FilterExecutedModel? executed = null;

        foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.Name == FunctionExecuting)
            {
                executing = new FilterExecutingModel(
                    IsAsyncRequired(method),
                    ResolveReturnType(method) is not null);
            }
            else if (method.Name == FunctionExecuted)
            {
                executed = new FilterExecutedModel(
                    IsAsyncRequired(method));
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
            IsAsyncRequired(symbol),
            symbol.Parameters.Select(static x => BuildParameterInfo(x)).ToList(),
            ResolveReturnType(symbol));
    }

    private static bool IsAsyncRequired(IMethodSymbol symbol)
    {
        if (symbol.IsAsync)
        {
            return true;
        }

        var fullName = symbol.ReturnType.ToDisplayString();
        if (fullName.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal) ||
            fullName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
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
            if (attributeName == FromQueryAttributeName)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery, false, FindKeyNameFromAttribute(attribute));
            }
            if (attributeName == FromBodyAttributeName)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromBody, ResolveNamedArgument(attribute, nameof(FromBodyAttribute.SkipValidate), false));
            }
            if (attributeName == FromRouteAttributeName)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromRoute, false, FindKeyNameFromAttribute(attribute));
            }
            if (attributeName == FromHeaderAttributeName)
            {
                return new ParameterModel(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromHeader, false, FindKeyNameFromAttribute(attribute));
            }
            if (attributeName == FromServicesAttributeName)
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

    private static T ResolveNamedArgument<T>(AttributeData attribute, string name, T defaultValue)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is T value)
            {
                return value;
            }
        }

        return defaultValue;
    }

    private static TypeModel BuildTypeInfo(ITypeSymbol symbol)
    {
        if (symbol.IsArrayType())
        {
            return new TypeModel(
                symbol.ToDisplayString(),
                true,
                false,
                null,
                true,
                BuildTypeInfo(symbol.GetArrayElementType()));
        }

        var isNullable = symbol.IsNullableType();
        return new TypeModel(
            symbol.ToDisplayString(),
            symbol.IsReferenceType || isNullable,
            isNullable,
            isNullable ? BuildTypeInfo(symbol.GetTypeArguments()[0]) : null,
            false,
            null);
    }
}
