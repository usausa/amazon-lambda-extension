#pragma warning disable IDE0060, IDE0042, SA1313
namespace AmazonLambdaExtension.Generator;
using System.Linq;

using AmazonLambdaExtension.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SourceGenerateHelper;

internal static class ModelBuilder
{
    private const string ServiceResolverAttributeName = "AmazonLambdaExtension.Annotations.ServiceResolverAttribute";
    private const string FilterAttributeName = "AmazonLambdaExtension.Annotations.FilterAttribute`1";
    private const string HttpApiAttributeName = "AmazonLambdaExtension.Annotations.HttpApiAttribute";
    private const string FunctionUrlAttributeName = "AmazonLambdaExtension.Annotations.FunctionUrlAttribute";
    private const string HttpApiAuthorizerAttributeName = "AmazonLambdaExtension.Annotations.HttpApiAuthorizerAttribute";
    private const string EventAttributeName = "AmazonLambdaExtension.Annotations.EventAttribute";

    private const string FromBodyAttributeName = "AmazonLambdaExtension.Annotations.FromBodyAttribute";
    private const string FromQueryAttributeName = "AmazonLambdaExtension.Annotations.FromQueryAttribute";
    private const string FromHeaderAttributeName = "AmazonLambdaExtension.Annotations.FromHeaderAttribute";
    private const string FromRouteAttributeName = "AmazonLambdaExtension.Annotations.FromRouteAttribute";
    private const string FromServicesAttributeName = "AmazonLambdaExtension.Annotations.FromServicesAttribute";
    private const string FromCustomAuthorizerAttributeName = "AmazonLambdaExtension.Annotations.FromCustomAuthorizerAttribute";

    private const string IHttpResultFullName = "AmazonLambdaExtension.APIGateway.IHttpResult";
    private const string IAuthorizerResultFullName = "AmazonLambdaExtension.APIGateway.IAuthorizerResult";
    private const string ILambdaFilterFullName = "AmazonLambdaExtension.Filters.ILambdaFilter";
    private const string IServiceCollectionFullName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

    public static Result<LambdaModel> BuildLambdaModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (ClassDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.TargetSymbol;

        // ALE0001: must be partial
        var isPartial = syntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            return Results.Error<LambdaModel>(new DiagnosticInfo(
                Diagnostics.NotPartialClass, syntax.GetLocation(), symbol.Name));
        }

        var ns = string.IsNullOrEmpty(symbol.ContainingNamespace.Name)
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var functionType = MakeTypeRef(symbol);

        // Constructor parameters
        var ctor = symbol.InstanceConstructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        var ctorParams = ctor != null
            ? ctor.Parameters.Select(static p => MakeTypeRef(p.Type)).ToArray()
            : [];

        // ALE0010: constructor params but no [ServiceResolver]
        ServiceResolverModel? serviceResolver = null;
        var serviceResolverAttr = symbol.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == ServiceResolverAttributeName);
        if (serviceResolverAttr != null)
        {
            if (serviceResolverAttr.ConstructorArguments.Length > 0 &&
                serviceResolverAttr.ConstructorArguments[0].Value is INamedTypeSymbol resolverType)
            {
                // ALE0011: must have public static IServiceCollection ConfigureServices()
                var configureMethod = resolverType.GetMembers("ConfigureServices")
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(static m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public
                        && m.Parameters.Length == 0
                        && m.ReturnType.ToDisplayString() == IServiceCollectionFullName);

                if (configureMethod == null)
                {
                    return Results.Error<LambdaModel>(new DiagnosticInfo(
                        Diagnostics.InvalidServiceResolverType, syntax.GetLocation(), resolverType.ToDisplayString()));
                }

                serviceResolver = new ServiceResolverModel(MakeTypeRef(resolverType));
            }
        }
        else if (ctorParams.Length > 0)
        {
            return Results.Error<LambdaModel>(new DiagnosticInfo(
                Diagnostics.MissingServiceResolver, syntax.GetLocation(), symbol.Name));
        }

        // Filters: collect [Filter<TFilter>(Order=N)] attributes, sort by (Order ASC, DeclarationIndex ASC)
        var filterAttrs = symbol.GetAttributes()
            .Select(static (a, i) => (Attr: a, Index: i))
            .Where(static x => IsFilterAttribute(x.Attr))
            .ToArray();

        var sortedFilters = filterAttrs
            .OrderBy(static x => GetFilterOrder(x.Attr))
            .ThenBy(static x => x.Index)
            .Select((x, idx) =>
            {
                var filterType = x.Attr.AttributeClass!.TypeArguments[0];
                return new FilterDescriptorModel(idx, MakeTypeRef(filterType), GetFilterOrder(x.Attr));
            })
            .ToArray();

        // Check ALE0012 for each filter
        var diagnostics = new List<DiagnosticInfo>();
        foreach (var fd in sortedFilters)
        {
            var filterAttr = filterAttrs.First(x => GetFilterOrder(x.Attr) == fd.Order);
            var filterTypeArg = filterAttr.Attr.AttributeClass!.TypeArguments[0];
            if (filterTypeArg is INamedTypeSymbol filterTypeSym && !ImplementsInterface(filterTypeSym, ILambdaFilterFullName))
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.FilterNotImplementILambdaFilter, syntax.GetLocation(), fd.FilterType.FullName));
            }
        }

        if (diagnostics.Count > 0)
        {
            return Results.Error<LambdaModel>(diagnostics[0]);
        }

        // Handlers: scan methods
        var handlers = new List<HandlerModel>();
        foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary || member.IsStatic)
            {
                continue;
            }

            var handlerResult = BuildHandlerModel(member, symbol, diagnostics);
            if (handlerResult == null)
            {
                if (diagnostics.Count > 0)
                {
                    return Results.Error<LambdaModel>(diagnostics[0]);
                }
                continue;
            }

            handlers.Add(handlerResult);
        }

        return Results.Success(new LambdaModel(
            ns,
            symbol.Name,
            symbol.IsValueType,
            functionType,
            new EquatableArray<TypeRefModel>(ctorParams),
            serviceResolver,
            new EquatableArray<FilterDescriptorModel>(sortedFilters),
            new EquatableArray<HandlerModel>(handlers.ToArray())));
    }

    private static bool IsFilterAttribute(AttributeData attr)
    {
        var attrClass = attr.AttributeClass;
        if (attrClass == null)
        {
            return false;
        }
        if (attrClass.IsGenericType)
        {
            var original = attrClass.OriginalDefinition;
            var ns = original.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            return ns + "." + original.MetadataName == FilterAttributeName;
        }
        return false;
    }

    private static int GetFilterOrder(AttributeData attr)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(static a => a.Key == "Order");
        if (namedArg.Value.Value is int order)
        {
            return order;
        }
        return 0;
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, string interfaceFullName)
    {
        return type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceFullName);
    }

    private static HandlerModel? BuildHandlerModel(IMethodSymbol method, INamedTypeSymbol _containingType, List<DiagnosticInfo> diagnostics)
    {
        HandlerKind? kind = null;
        HttpApiHandlerOptions? httpApiOptions = null;
        AuthorizerHandlerOptions? authorizerOptions = null;
        var handlerAttrCount = 0;

        foreach (var attr in method.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName == HttpApiAttributeName)
            {
                handlerAttrCount++;
                kind = HandlerKind.HttpApi;
                var httpMethod = attr.ConstructorArguments.Length > 0 ? (int)(attr.ConstructorArguments[0].Value ?? 0) : 0;
                var template = attr.ConstructorArguments.Length > 1 ? attr.ConstructorArguments[1].Value as string : null;
                var authorizerName = attr.NamedArguments.FirstOrDefault(static a => a.Key == "Authorizer").Value.Value as string;
                httpApiOptions = new HttpApiHandlerOptions(httpMethod, template, authorizerName);
            }
            else if (attrName == FunctionUrlAttributeName)
            {
                handlerAttrCount++;
                kind = HandlerKind.FunctionUrl;
                httpApiOptions = new HttpApiHandlerOptions(0, null, null);
            }
            else if (attrName == HttpApiAuthorizerAttributeName)
            {
                handlerAttrCount++;
                kind = HandlerKind.HttpApiAuthorizer;
                var enableSimple = attr.NamedArguments.FirstOrDefault(static a => a.Key == "EnableSimpleResponses").Value.Value;
                var enableSimpleBool = enableSimple is not false;
                authorizerOptions = new AuthorizerHandlerOptions(enableSimpleBool);
            }
            else if (attrName == EventAttributeName)
            {
                handlerAttrCount++;
                kind = HandlerKind.Event;
            }
        }

        if (kind == null)
        {
            return null;
        }

        // ALE0003: 複数のハンドラー属性
        if (handlerAttrCount > 1)
        {
            var loc = method.Locations.Length > 0 ? method.Locations[0] : null;
            diagnostics.Add(new DiagnosticInfo(Diagnostics.MultipleHandlerAttributes, loc, method.Name));
            return null;
        }

        // Parameters
        var parameters = new List<ParameterModel>();
        foreach (var param in method.Parameters)
        {
            // ALE0005: [Event] ハンドラに [FromBody] は使用不可
            if (kind == HandlerKind.Event && param.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == FromBodyAttributeName))
            {
                var loc = method.Locations.Length > 0 ? method.Locations[0] : null;
                diagnostics.Add(new DiagnosticInfo(Diagnostics.FromBodyOnEventHandler, loc, method.Name));
                return null;
            }

            var paramModel = BuildParameterModel(param, kind.Value);
            parameters.Add(paramModel);
        }

        // Result type analysis
        var returnType = method.ReturnType;
        TypeRefModel? resultType = null;
        var isAsync = false;

        if (returnType is INamedTypeSymbol namedReturn)
        {
            if (namedReturn.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>" ||
                namedReturn.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>")
            {
                isAsync = true;
                var inner = namedReturn.TypeArguments[0];
                resultType = MakeTypeRef(inner);
            }
            else if (namedReturn.ToDisplayString() == "System.Threading.Tasks.Task" ||
                     namedReturn.ToDisplayString() == "System.Threading.Tasks.ValueTask")
            {
                isAsync = true;
                resultType = null;
            }
            else if (namedReturn.ToDisplayString() == "void")
            {
                resultType = null;
            }
            else
            {
                resultType = MakeTypeRef(namedReturn);
            }
        }
        else
        {
            resultType = MakeTypeRef(returnType);
        }

        var returnsHttpResult = resultType != null && IsImplementing(method.ReturnType, IHttpResultFullName);
        var returnsAuthorizerResult = resultType != null && IsImplementing(method.ReturnType, IAuthorizerResultFullName);

        // ALE0007: [HttpApiAuthorizer] の戻り値型が IAuthorizerResult でない
        if (kind == HandlerKind.HttpApiAuthorizer && !returnsAuthorizerResult)
        {
            var loc = method.Locations.Length > 0 ? method.Locations[0] : null;
            diagnostics.Add(new DiagnosticInfo(Diagnostics.AuthorizerInvalidReturnType, loc, method.Name));
            return null;
        }

        return new HandlerModel(
            method.Name,
            kind.Value,
            isAsync,
            resultType,
            returnsHttpResult,
            returnsAuthorizerResult,
            new EquatableArray<ParameterModel>(parameters.ToArray()),
            httpApiOptions,
            authorizerOptions);
    }

    private static bool IsImplementing(ITypeSymbol type, string interfaceName)
    {
        if (type is INamedTypeSymbol named)
        {
            if (named.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>" ||
                named.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>")
            {
                return IsImplementing(named.TypeArguments[0], interfaceName);
            }
            return named.ToDisplayString() == interfaceName ||
                   named.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName);
        }
        return false;
    }

    private static ParameterModel BuildParameterModel(IParameterSymbol param, HandlerKind handlerKind)
    {
        var paramType = param.Type;
        var bindingKind = ParameterBindingKind.FromQuery;
        var key = param.Name;
        var converterMethod = GetConverterMethod(paramType);

        // Check for explicit binding attributes
        foreach (var attr in param.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName == FromBodyAttributeName)
            {
                bindingKind = ParameterBindingKind.FromBody;
                break;
            }
            if (attrName == FromQueryAttributeName)
            {
                bindingKind = ParameterBindingKind.FromQuery;
                var nameArg = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                if (!string.IsNullOrEmpty(nameArg))
                {
                    key = nameArg!;
                }

                break;
            }
            if (attrName == FromHeaderAttributeName)
            {
                bindingKind = ParameterBindingKind.FromHeader;
                var nameArg = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                if (!string.IsNullOrEmpty(nameArg))
                {
                    key = nameArg!;
                }

                break;
            }
            if (attrName == FromRouteAttributeName)
            {
                bindingKind = ParameterBindingKind.FromRoute;
                var nameArg = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                if (!string.IsNullOrEmpty(nameArg))
                {
                    key = nameArg!;
                }

                break;
            }
            if (attrName == FromServicesAttributeName)
            {
                bindingKind = ParameterBindingKind.FromServices;
                break;
            }
            if (attrName == FromCustomAuthorizerAttributeName)
            {
                bindingKind = ParameterBindingKind.FromCustomAuthorizer;
                var nameArg = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                if (!string.IsNullOrEmpty(nameArg))
                {
                    key = nameArg!;
                }

                break;
            }
        }

        // Auto-detect special types when no explicit binding
        if (!param.GetAttributes().Any(HasBindingAttribute))
        {
            var typeName = paramType.ToDisplayString();
            if (typeName == "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest")
            {
                bindingKind = ParameterBindingKind.Request;
                converterMethod = string.Empty;
            }
            else if (typeName == "Amazon.Lambda.Core.ILambdaContext")
            {
                bindingKind = ParameterBindingKind.Context;
                converterMethod = string.Empty;
            }
            else if (handlerKind == HandlerKind.Event)
            {
                // For Event handlers, non-context parameters without explicit binding are the event payload
                bindingKind = ParameterBindingKind.Request;
                converterMethod = string.Empty;
            }
        }

        var skipValidation = false;
        var fromBodyAttr = param.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == FromBodyAttributeName);
        if (fromBodyAttr != null)
        {
            var skipArg = fromBodyAttr.NamedArguments.FirstOrDefault(static a => a.Key == "SkipValidate").Value.Value;
            skipValidation = skipArg is true;
        }

        return new ParameterModel(
            param.Name,
            MakeTypeRef(paramType),
            bindingKind,
            key,
            converterMethod,
            skipValidation);
    }

    private static bool HasBindingAttribute(AttributeData attr)
    {
        var name = attr.AttributeClass?.ToDisplayString();
        return name == FromBodyAttributeName || name == FromQueryAttributeName ||
               name == FromHeaderAttributeName || name == FromRouteAttributeName ||
               name == FromServicesAttributeName || name == FromCustomAuthorizerAttributeName;
    }

    private static string GetConverterMethod(ITypeSymbol type)
    {
        // Array type
        if (type is IArrayTypeSymbol arr)
        {
            return GetConverterMethod(arr.ElementType);
        }

        // Nullable<T>
        if (type is INamedTypeSymbol named && named.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
        {
            return GetConverterMethod(named.TypeArguments[0]);
        }

        var fullName = type.ToDisplayString();
        return fullName switch
        {
            "bool" or "System.Boolean" => "TryToBoolean",
            "byte" or "System.Byte" => "TryToByte",
            "sbyte" or "System.SByte" => "TryToSByte",
            "short" or "System.Int16" => "TryToInt16",
            "ushort" or "System.UInt16" => "TryToUInt16",
            "int" or "System.Int32" => "TryToInt32",
            "uint" or "System.UInt32" => "TryToUInt32",
            "long" or "System.Int64" => "TryToInt64",
            "ulong" or "System.UInt64" => "TryToUInt64",
            "float" or "System.Single" => "TryToSingle",
            "double" or "System.Double" => "TryToDouble",
            "decimal" or "System.Decimal" => "TryToDecimal",
            "char" or "System.Char" => "TryToChar",
            "System.DateTime" => "TryToDateTime",
            "System.DateTimeOffset" => "TryToDateTimeOffset",
            "System.DateOnly" => "TryToDateOnly",
            "System.TimeOnly" => "TryToTimeOnly",
            "System.TimeSpan" => "TryToTimeSpan",
            "System.Guid" => "TryToGuid",
            "string" or "System.String" => string.Empty,  // no conversion needed
            _ when type.TypeKind == TypeKind.Enum => "TryToEnum",
            _ => string.Empty
        };
    }

    internal static TypeRefModel MakeTypeRef(ITypeSymbol type)
    {
        var isNullable = false;
        TypeRefModel? underlyingType = null;

        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
        {
            isNullable = true;
            underlyingType = MakeTypeRef(namedType.TypeArguments[0]);
        }

        if (type is IArrayTypeSymbol arr)
        {
            return new TypeRefModel(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                type.NullableAnnotation == NullableAnnotation.Annotated,
                false,
                null,
                true,
                MakeTypeRef(arr.ElementType));
        }

        return new TypeRefModel(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            type.NullableAnnotation == NullableAnnotation.Annotated,
            isNullable,
            underlyingType,
            false,
            null);
    }
}
