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
    private const string LambdaAttribute = "AmazonLambdaExtension.LambdaAttribute";
    private const string HttpApiAttribute = "AmazonLambdaExtension.HttpApiAttribute";

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
                context.AddSource($"{handlerInfo.WrapperClassName}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            }
        }
    }

    //--------------------------------------------------------------------------------
    // Builder
    //--------------------------------------------------------------------------------

    private static FunctionInfo BuildFunctionInfo(ITypeSymbol symbol)
    {
        // TODO GetParameter ITypeSymbol's
        //    public TypeInfo[] ConstructorParameters { get; set; }
        // TODO GetAttribute and parameter ITypeSymbol
        //    public TypeInfo ServiceLocator { get; set; }

        //var serviceResolverData = classSymbol.GetAttributes()
        //    .FirstOrDefault(x => x.AttributeClass!.ToDisplayString() == "AmazonLambdaExtension.ServiceResolverAttribute");
        //if (serviceResolverData is not null)
        //{
        //    var argument = serviceResolverData.ConstructorArguments[0];
        //    var value = (ITypeSymbol)argument.Value!;

        //    foreach (var member in value.GetMembers())
        //    {
        //        // TODO ISymbol
        //        Debug.WriteLine(member);
        //    }

        //    Debug.WriteLine(value);
        //}

        // TODO enum method, return type, parameter type, generic
        //    public string FindService(TypeInfo type)
        //    public string FindSerializer()

        return new FunctionInfo
        {
            Function = BuildTypeInfo(symbol)
        };
    }

    private static HandlerInfo BuildHandlerInfo(IMethodSymbol symbol)
    {
        var parameters = new List<ParameterInfo>();
        //    public ParameterInfo[] Parameters { get; set; }
        //    public TypeInfo ResultType { get; set; }
        return new HandlerInfo
        {
            ContainingNamespace = symbol.ContainingNamespace.ToDisplayString(),
            WrapperClassName = $"{symbol.ContainingType.Name}_{symbol.Name}_Generated",
            IsAsync = symbol.IsAsync,
            MethodName = symbol.Name,
            Parameters = parameters
        };
    }

    //public class ParameterInfo
    //    public string Name { get; set; }
    //    public TypeInfo Type { get; set; }
    //    public ParameterType ParameterType { get; set; }

    private static TypeInfo BuildTypeInfo(ITypeSymbol symbol)
    {
        // TODO    public bool IsMultiType { get; set; }
        return new TypeInfo
        {
            FullName = symbol.ToDisplayString(),
            IsNullable = symbol.IsReferenceType || symbol.IsNullable()
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
