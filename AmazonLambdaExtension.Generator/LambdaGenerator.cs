namespace AmazonLambdaExtension.Generator;

using System.Text;

using AmazonLambdaExtension.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

[Generator]
public sealed class LambdaGenerator : IIncrementalGenerator
{
    private const string LambdaAttributeFullName = "AmazonLambdaExtension.Annotations.LambdaAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                LambdaAttributeFullName,
                static (syntax, _) => syntax is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, _) => LambdaModelBuilder.BuildLambdaModel(ctx))
            .Collect();

        context.RegisterImplementationSourceOutput(provider, static (ctx, results) => Execute(ctx, results));
    }

    private static void Execute(
        SourceProductionContext context,
        System.Collections.Immutable.ImmutableArray<Result<LambdaModel>> results)
    {
        foreach (var diagnostic in results.SelectError())
        {
            context.ReportDiagnostic(diagnostic);
        }

        var builder = new SourceBuilder();

        foreach (var model in results.SelectValue())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // Emit shared static fields once per class
            builder.Clear();
            LambdaSourceBuilder.BuildShared(builder, model);
            context.AddSource(
                MakeFilename(model.Namespace, model.ClassName, "__shared__"),
                SourceText.From(builder.ToString(), Encoding.UTF8));

            foreach (var handler in model.Handlers)
            {
                builder.Clear();
                LambdaSourceBuilder.Build(builder, model, handler);

                var filename = MakeFilename(model.Namespace, model.ClassName, handler.MethodName);
                context.AddSource(filename, SourceText.From(builder.ToString(), Encoding.UTF8));
            }
        }
    }

    private static string MakeFilename(string ns, string className, string methodName)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return $"{className}__{methodName}.g.cs";
        }
        return $"{ns}.{className}__{methodName}.g.cs";
    }
}
