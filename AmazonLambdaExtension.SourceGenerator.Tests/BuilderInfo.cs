namespace AmazonLambdaExtension.SourceGenerator.Tests;

using System.Reflection;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using AmazonLambdaExtension.Annotations;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed class BuilderInfo
{
    private const string LambdaAttribute = "AmazonLambdaExtension.Annotations.LambdaAttribute";
    private const string ApiAttribute = "AmazonLambdaExtension.Annotations.ApiAttribute";

    public ITypeSymbol Class { get; }

    public IMethodSymbol Method { get; }

    private BuilderInfo(ITypeSymbol @class, IMethodSymbol method)
    {
        Class = @class;
        Method = method;
    }

    public static BuilderInfo Create(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", new[] { tree })
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(LambdaAttribute).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(APIGatewayProxyRequest).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ILambdaContext).GetTypeInfo().Assembly.Location));

        foreach (var classDeclarationSyntax in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var classSemantic = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            var classSymbol = classSemantic.GetDeclaredSymbol(classDeclarationSyntax)!;
            if (classSymbol.GetAttributes().Any(static x => x.AttributeClass!.ToDisplayString() == LambdaAttribute))
            {
                foreach (var member in classDeclarationSyntax.Members)
                {
                    if (member is not MethodDeclarationSyntax methodDeclarationSyntax)
                    {
                        continue;
                    }

                    var methodSemantic = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);
                    var methodSymbol = methodSemantic.GetDeclaredSymbol(methodDeclarationSyntax)!;
                    if (methodSymbol.GetAttributes().Any(static x => x.AttributeClass!.ToDisplayString() == ApiAttribute))
                    {
                        return new BuilderInfo(classSymbol, methodSymbol);
                    }
                }
            }
        }

        throw new ArgumentException("Invalid source.", nameof(source));
    }
}
