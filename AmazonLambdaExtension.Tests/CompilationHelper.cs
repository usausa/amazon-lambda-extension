namespace AmazonLambdaExtension;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Amazon.Lambda.APIGatewayEvents;
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

    public static IReadOnlyList<string> GetProblemIds(string source) =>
        [.. Runner.GetProblems(source).Select(static x => x.Id)];

    public static Assembly LoadAssembly(string source)
    {
        var result = Runner.Run(source);
        Assert.True(result.Problems.Count == 0, String.Join(Environment.NewLine, result.Problems.Select(static x => x.ToString())));

        using var stream = new MemoryStream();
        var emitResult = result.OutputCompilation.Emit(stream);
        Assert.True(emitResult.Success, String.Join(Environment.NewLine, emitResult.Diagnostics.Select(static x => x.ToString())));

        stream.Position = 0;
        return new AssemblyLoadContext(null, isCollectible: true).LoadFromStream(stream);
    }

    public static Task<APIGatewayHttpApiV2ProxyResponse> InvokeAsync(Assembly assembly, string handler, APIGatewayHttpApiV2ProxyRequest request)
    {
        var method = assembly.GetType("Test.Function", throwOnError: true)!.GetMethod(handler)!;
        return (Task<APIGatewayHttpApiV2ProxyResponse>)method.Invoke(null, [request, new TestLambdaContext()])!;
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

    private sealed class TestLambdaContext : ILambdaContext
    {
        public string AwsRequestId => "request";

        public IClientContext ClientContext => null!;

        public string FunctionName => "function";

        public string FunctionVersion => "1";

        public ICognitoIdentity Identity => null!;

        public string InvokedFunctionArn => "arn";

        public ILambdaLogger Logger { get; } = new TestLambdaLogger();

        public string LogGroupName => "group";

        public string LogStreamName => "stream";

        public int MemoryLimitInMB => 128;

        public TimeSpan RemainingTime => TimeSpan.FromMinutes(1);
    }

    private sealed class TestLambdaLogger : ILambdaLogger
    {
        public void Log(string message)
        {
        }

        public void LogLine(string message)
        {
        }
    }
}
