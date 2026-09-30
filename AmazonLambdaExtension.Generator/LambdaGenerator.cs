namespace AmazonLambdaExtension.Generator;

using System.Collections.Immutable;

using AmazonLambdaExtension.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                static (syntax, _) => IsClassSyntax(syntax),
                static (ctx, _) => LambdaModelBuilder.BuildLambdaModel(ctx));
        var treeProvider = context.ForAttributeWithMetadataNameSyntaxTrees(
            LambdaAttributeFullName,
            static (syntax, _) => IsClassSyntax(syntax));

        var collected = provider.Collect();
        context.RegisterSourceOutput(
            collected.Combine(treeProvider),
            static (ctx, input) => ctx.ReportDiagnostics(SelectDiagnostics(input.Left), input.Right));
        var models = collected.SelectMany(static (results, _) => SelectModels(results));
        context.RegisterImplementationSourceOutput(models, static (ctx, model) => Execute(ctx, model));
    }

    private static bool IsClassSyntax(SyntaxNode syntax) =>
        syntax is ClassDeclarationSyntax or RecordDeclarationSyntax;

    private static string GetTypeName(LambdaModel model) =>
        String.IsNullOrEmpty(model.Namespace) ? model.ClassName : $"{model.Namespace}.{model.ClassName}";

    private static string GetHintKey(LambdaModel model) =>
        HintNameBuilder.Build(model.Namespace, model.ClassName);

    private static IEnumerable<DiagnosticInfo> SelectDiagnostics(ImmutableArray<Result<LambdaModel>> results) =>
        results.SelectError()
            .Concat(FindHintNameCollisions(results).Values)
            .Concat(results.SelectValue().SelectMany(static x => FindHandlerCollisions(x).Values))
            .Distinct();

    private static IEnumerable<LambdaModel> SelectModels(ImmutableArray<Result<LambdaModel>> results)
    {
        var collisions = FindHintNameCollisions(results);
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in results.SelectValue())
        {
            var key = GetHintKey(model);
            if (!collisions.ContainsKey(key) && emitted.Add(key))
            {
                yield return model;
            }
        }
    }

    private static Dictionary<string, DiagnosticInfo> FindHintNameCollisions(ImmutableArray<Result<LambdaModel>> results)
    {
        var collisions = new Dictionary<string, DiagnosticInfo>(StringComparer.Ordinal);
        var firsts = new Dictionary<string, LambdaModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var model in results.SelectValue().OrderBy(GetHintKey, StringComparer.Ordinal))
        {
            var key = GetHintKey(model);
            if (!firsts.TryGetValue(key, out var first))
            {
                firsts.Add(key, model);
            }
            else if ((GetHintKey(first) != key) && !collisions.ContainsKey(key))
            {
                collisions.Add(key, new DiagnosticInfo(Diagnostics.HintNameCollision, (Location?)null, GetTypeName(model), GetTypeName(first)));
            }
        }

        return collisions;
    }

    private static Dictionary<string, DiagnosticInfo> FindHandlerCollisions(LambdaModel model)
    {
        var collisions = new Dictionary<string, DiagnosticInfo>(StringComparer.Ordinal);
        var firsts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["__shared__"] = "__shared__" };
        foreach (var handler in model.Handlers.OrderBy(static x => x.MethodName, StringComparer.Ordinal))
        {
            if (!firsts.TryGetValue(handler.MethodName, out var first))
            {
                firsts.Add(handler.MethodName, handler.MethodName);
            }
            else if ((first != handler.MethodName) && !collisions.ContainsKey(handler.MethodName))
            {
                collisions.Add(handler.MethodName, new DiagnosticInfo(Diagnostics.HintNameCollision, (Location?)null, $"{GetTypeName(model)}.{handler.MethodName}", $"{GetTypeName(model)}.{first}"));
            }
        }

        return collisions;
    }

    private static void Execute(SourceProductionContext context, LambdaModel model)
    {
        var builder = new SourceBuilder();

        LambdaSourceBuilder.BuildShared(builder, model);
        context.AddSource(HintNameBuilder.Build(model.Namespace, model.ClassName, "__shared__"), builder);

        var collisions = FindHandlerCollisions(model);
        foreach (var handler in model.Handlers)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (collisions.ContainsKey(handler.MethodName))
            {
                continue;
            }

            builder.Clear();
            LambdaSourceBuilder.Build(builder, model, handler);
            context.AddSource(HintNameBuilder.Build(model.Namespace, model.ClassName, handler.MethodName), builder);
        }
    }
}
