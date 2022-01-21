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
    private const string EventAttribute = "AmazonLambdaExtension.Annotations.EventAttribute";

    private enum HandlerType
    {
        None,
        HttpApi,
        Event
    }

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

        foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes))
        {
            if ((context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol) &&
                (attributeSymbol.ContainingType.ToDisplayString() == LambdaAttribute))
            {
                return classDeclarationSyntax;
            }
        }

        return null;
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
                var methodSymbol = methodSemantic.GetDeclaredSymbol(methodDeclarationSyntax)!;
                var handlerType = ResolveHandlerType(methodSymbol);

                if (handlerType == HandlerType.None)
                {
                    return;
                }

                var handler = ModelBuilder.BuildHandlerInfo((IMethodSymbol)methodSymbol);

                if (handlerType == HandlerType.HttpApi)
                {
                    // Generate wrapper
                    var template = new HttpApiTemplate(function, handler);
                    var sourceText = template.TransformText();
                    context.AddSource($"{handler.WrapperClass}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
                else if (handlerType == HandlerType.Event)
                {
                    // Generate wrapper
                    var template = new EventTemplate(function, handler);
                    var sourceText = template.TransformText();
                    context.AddSource($"{handler.WrapperClass}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
            }
        }
    }

    private static HandlerType ResolveHandlerType(ISymbol symbol)
    {
        foreach (var name in symbol.GetAttributes().Select(attribute => attribute.AttributeClass!.ToDisplayString()))
        {
            if (name == HttpApiAttribute)
            {
                return HandlerType.HttpApi;
            }
            if (name == EventAttribute)
            {
                return HandlerType.Event;
            }
        }

        return HandlerType.None;
    }
}
