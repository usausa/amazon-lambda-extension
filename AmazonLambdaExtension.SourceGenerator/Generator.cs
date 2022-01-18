namespace AmazonLambdaExtension.SourceGenerator;

using System.Collections.Immutable;
using System.Text;

using AmazonLambdaExtension.SourceGenerator.Templates;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class TestGenerator : IIncrementalGenerator
{
    private const string LambdaAttribute = "AmazonLambdaExtension.Annotations.LambdaAttribute";
    private const string HttpApiAttribute = "AmazonLambdaExtension.Annotations.HttpApiAttribute";

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
            var function = ModelBuilder.BuildFunctionInfo((ITypeSymbol)classSymbol);

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
                var handler = ModelBuilder.BuildHandlerInfo((IMethodSymbol)methodSymbol);

                // Generate wrapper
                var template = new LambdaTemplate(function, handler);
                var sourceText = template.TransformText();
                context.AddSource($"{handler.WrapperClass}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        }
    }

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
