namespace AmazonLambdaExtension.SourceGenerator;

using System.Collections.Immutable;
using System.Text;

using AmazonLambdaExtension.SourceGenerator.Models;
using AmazonLambdaExtension.SourceGenerator.Templates;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using TypeInfo = AmazonLambdaExtension.SourceGenerator.Models.TypeInfo;

[Generator]
public sealed class TestGenerator : IIncrementalGenerator
{
    private const string LambdaAttribute = "AmazonLambdaExtension.Annotations.LambdaAttribute";
    private const string HttpApiAttribute = "AmazonLambdaExtension.Annotations.HttpApiAttribute";

    private const string ServiceResolverAttribute = "AmazonLambdaExtension.Annotations.ServiceResolverAttribute";

    private const string FromQueryAttribute = "AmazonLambdaExtension.Annotations.FromQueryAttribute";
    private const string FromBodyAttribute = "AmazonLambdaExtension.Annotations.FromBodyAttribute";
    private const string FromRouteAttribute = "AmazonLambdaExtension.Annotations.FromRouteAttribute";
    private const string FromHeaderAttribute = "AmazonLambdaExtension.Annotations.FromHeaderAttribute";
    private const string FromServiceAttribute = "AmazonLambdaExtension.Annotations.FromServiceAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => IsTargetSyntax(s),
                static (ctx, _) => GetTargetSyntax(ctx))
            .SelectMany(static (x, _) => x is not null ? ImmutableArray.Create(x) : ImmutableArray<ClassDeclarationSyntax>.Empty);
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterImplementationSourceOutput(compilationAndClasses, static (spc, source) => Execute(spc, source.Item1, source.Item2));
    }

    private static bool IsTargetSyntax(SyntaxNode node) =>
        node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0;

    private static ClassDeclarationSyntax? GetTargetSyntax(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        return HasAttribute(context.SemanticModel, classDeclarationSyntax.AttributeLists, LambdaAttribute) ? classDeclarationSyntax : null;
    }

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var classDeclarationSyntax in classes)
        {
            // Check cancel
            context.CancellationToken.ThrowIfCancellationRequested();

            // Build metadata
            var classSemantic = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            var classSymbol = classSemantic.GetDeclaredSymbol(classDeclarationSyntax)!;
            var functionInfo = BuildFunctionInfo((ITypeSymbol)classSymbol);

            foreach (var member in classDeclarationSyntax.Members)
            {
                if (member is not MethodDeclarationSyntax methodDeclarationSyntax)
                {
                    continue;
                }

                // Check cancel
                context.CancellationToken.ThrowIfCancellationRequested();

                // Build metadata
                var methodSemantic = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);

                if (!HasAttribute(methodSemantic, methodDeclarationSyntax.AttributeLists, HttpApiAttribute))
                {
                    continue;
                }

                var methodSymbol = methodSemantic.GetDeclaredSymbol(methodDeclarationSyntax)!;
                var handlerInfo = BuildHandlerInfo((IMethodSymbol)methodSymbol);

                var template = new LambdaTemplate(functionInfo, handlerInfo);
                var sourceText = template.TransformText();
                context.AddSource($"{handlerInfo.WrapperClass}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Builder
    //--------------------------------------------------------------------------------

    private static FunctionInfo BuildFunctionInfo(ITypeSymbol symbol)
    {
        var ctor = symbol.GetConstructors()
            .OrderByDescending(x => x.Parameters.Length)
            .First();
        var serviceLocator = symbol.GetAttributes()
            .Where(x => x.AttributeClass!.ToDisplayString() == ServiceResolverAttribute)
            .Select(x => (ITypeSymbol)x.ConstructorArguments[0].Value!)
            .FirstOrDefault();

        return new FunctionInfo(
            BuildTypeInfo(symbol),
            ctor.Parameters.Select(static x => BuildTypeInfo(x.Type)).ToList(),
            serviceLocator is not null ? BuildTypeInfo(serviceLocator) : null);
    }

    private static HandlerInfo BuildHandlerInfo(IMethodSymbol symbol)
    {
        return new HandlerInfo(
            symbol.ContainingNamespace.ToDisplayString(),
            $"{symbol.ContainingType.Name}_{symbol.Name}_Generated",
            symbol.Name,
            symbol.IsAsync,
            symbol.Parameters.Select(static x => BuildParameterInfo(x)).ToList(),
            ResolveReturnType(symbol));
    }

    private static TypeInfo? ResolveReturnType(IMethodSymbol symbol)
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

    private static ParameterInfo BuildParameterInfo(IParameterSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var name = attribute.AttributeClass!.ToDisplayString();
            if (name == FromQueryAttribute)
            {
                return new ParameterInfo(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery, FindKeyNameFromAttribute(attribute));
            }
            if (name == FromBodyAttribute)
            {
                return new ParameterInfo(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromBody);
            }
            if (name == FromRouteAttribute)
            {
                return new ParameterInfo(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromRoute, FindKeyNameFromAttribute(attribute));
            }
            if (name == FromHeaderAttribute)
            {
                return new ParameterInfo(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromHeader, FindKeyNameFromAttribute(attribute));
            }
            if (name == FromServiceAttribute)
            {
                return new ParameterInfo(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromService);
            }
        }

        return new ParameterInfo(symbol.Name, BuildTypeInfo(symbol.Type), ParameterType.FromQuery);
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

    private static TypeInfo BuildTypeInfo(ITypeSymbol symbol)
    {
        if (symbol.IsArrayType())
        {
            return new TypeInfo(symbol.ToDisplayString(), true, true, BuildTypeInfo(symbol.GetArrayElementType()));
        }

        return new TypeInfo(symbol.ToDisplayString(), symbol.IsReferenceType || symbol.IsNullableType(), false, null);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static bool HasAttribute(SemanticModel model, SyntaxList<AttributeListSyntax> list, string name)
    {
        foreach (var attributeListSyntax in list)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (model.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var fullName = attributeSymbol.ContainingType.ToDisplayString();
                if (fullName == name)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
