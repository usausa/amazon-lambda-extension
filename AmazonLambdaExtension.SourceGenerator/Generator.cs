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

            // TODO enum method, return type, parameter type, generic
            //    public string FindService(TypeInfo type)
            //    public string FindSerializer()

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
                context.AddSource($"{handlerInfo.WrapperClassName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
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

        // TODO ServiceLocatorInfo 別?
        return new FunctionInfo(
            BuildTypeInfo(symbol),
            ctor.Parameters.Select(static x => BuildTypeInfo(x.Type)).ToList(),
            serviceLocator is not null ? BuildTypeInfo(serviceLocator) : null,
            serviceLocator);
    }

    private static HandlerInfo BuildHandlerInfo(IMethodSymbol symbol)
    {
        return new HandlerInfo
        {
            ContainingNamespace = symbol.ContainingNamespace.ToDisplayString(),
            WrapperClassName = $"{symbol.ContainingType.Name}_{symbol.Name}_Generated",
            IsAsync = symbol.IsAsync,
            ResultType = ResolveReturnType(symbol),
            MethodName = symbol.Name,
            Parameters = symbol.Parameters.Select(static x => BuildParameterInfo(x)).ToList()
        };
    }

    private static TypeInfo? ResolveReturnType(IMethodSymbol symbol)
    {
        if (symbol.ReturnsVoid)
        {
            return null;
        }

        var fullName = symbol.ToDisplayString();
        if (fullName.StartsWith("System.Threading.Tasks.Task") ||
            fullName.StartsWith("System.Threading.Tasks.ValueTask"))
        {
            return symbol.ReturnType.IsGenericType() ? BuildTypeInfo(symbol.TypeArguments.First()) : null;
        }

        return BuildTypeInfo(symbol.ReturnType);
    }

    private static ParameterInfo BuildParameterInfo(IParameterSymbol symbol)
    {
        // TODO Attribute 1

        return new ParameterInfo
        {
            Name = symbol.Name,
            ParameterType = ParameterType.FromBody, // TODO
            Type = BuildTypeInfo(symbol.Type)
        };
    }

    // TODO IsGeneric -> Lookup用、型制約お見る？
    private static TypeInfo BuildTypeInfo(ITypeSymbol symbol)
    {
        // TODO    public bool IsMultiType { get; set; }
        return new TypeInfo
        {
            FullName = symbol.ToDisplayString(),
            IsNullable = symbol.IsReferenceType || symbol.IsNullable(),
            //IsMultiType = symbol.isa
            //IsMultiType = symbol.isa
        };
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
