namespace AmazonLambdaExtension;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

using SourceGenerateHelper.Testing;

internal static class CompilationHelper
{
    private static GeneratorTestRunner Runner => GeneratorTestRunner
        .For<LambdaGenerator>()
        .WithReference(typeof(LambdaAttribute).Assembly)
        .WithReference(typeof(ILambdaContext).Assembly)
        .WithReference(typeof(SQSEvent).Assembly)
        .WithReference(typeof(DefaultLambdaJsonSerializer).Assembly)
        .WithReference(typeof(IServiceCollection).Assembly);

    public static GeneratorResult RunGenerator(string source)
    {
        var result = Runner.Run(source);

        return new GeneratorResult(
            result.GeneratorDiagnostics.ToImmutableArray(),
            result.GeneratedSources);
    }

    public static void AssertNoGeneratorErrors(GeneratorResult result)
    {
        var errors = result.Diagnostics
            .Where(static x => x.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(errors.Length == 0, String.Join(Environment.NewLine, errors.Select(static x => x.ToString())));
    }

    public sealed record GeneratorResult(
        ImmutableArray<Diagnostic> Diagnostics,
        IReadOnlyDictionary<string, string> Sources);

    public static IncrementalRunResult RunIncremental(string source, string addedSource) =>
        Runner.WithTracking().RunIncremental(source, addedSource);
}
