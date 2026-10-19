namespace AmazonLambdaExtension;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    // [LambdaEntryPoint] 用: Main が呼ぶ RuntimeSupport を参照し、ハンドラー名に入るアセンブリ名を固定する
    // For [LambdaEntryPoint]: reference RuntimeSupport called by Main, and fix the assembly name used in handler names
    private static GeneratorTestRunner EntryPointRunner => Runner
        .WithReference(typeof(LambdaBootstrapBuilder).Assembly)
        .WithAssemblyName("TestAssembly");

    public static GeneratorResult RunGenerator(string source) =>
        RunGenerator(Runner, source);

    public static GeneratorResult RunEntryPointGenerator(string source) =>
        RunGenerator(EntryPointRunner, source);

    public static GeneratorResult RunEntryPointGenerator(string source, OutputKind outputKind, bool publishAot) =>
        RunGenerator(
            EntryPointRunner
                .WithOutputKind(outputKind)
                .WithGlobalOption("build_property.PublishAot", publishAot ? "true" : "false"),
            source);

    public static IReadOnlyList<string> GetProblemIds(string source) =>
        [.. Runner.GetProblems(source).Select(static x => x.Id)];

    // テストのプロセスは RuntimeSupport を読み込んでいるため、それを除いた参照でコンパイルを作る
    // The test process loads RuntimeSupport, so build a compilation whose references exclude it
    public static IReadOnlyList<string> GetDiagnosticIdsWithoutRuntimeSupport(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(static x => !Path.GetFileName(x).StartsWith("Amazon.Lambda.RuntimeSupport", StringComparison.OrdinalIgnoreCase))
            .Select(static x => MetadataReference.CreateFromFile(x));
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.Null(compilation.GetTypeByMetadataName(typeof(LambdaBootstrapBuilder).FullName!));

        var driver = CSharpGeneratorDriver.Create(new LambdaGenerator()).RunGenerators(compilation);
        return [.. driver.GetRunResult().Diagnostics.Select(static x => x.Id)];
    }

    public static Assembly LoadAssembly(string source) =>
        LoadAssembly(Runner, source);

    public static Assembly LoadEntryPointAssembly(string source) =>
        LoadAssembly(EntryPointRunner.WithOutputKind(OutputKind.ConsoleApplication), source);

    private static GeneratorResult RunGenerator(GeneratorTestRunner runner, string source)
    {
        var result = runner.Run(source);

        return new GeneratorResult(
            result.GeneratorDiagnostics.ToImmutableArray(),
            result.GeneratedSources);
    }

    private static Assembly LoadAssembly(GeneratorTestRunner runner, string source)
    {
        var result = runner.Run(source);
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

    public static Task<TResponse> InvokeAuthorizerAsync<TResponse>(Assembly assembly, string handler, APIGatewayCustomAuthorizerV2Request request)
    {
        var method = assembly.GetType("Test.Function", throwOnError: true)!.GetMethod(handler)!;
        return (Task<TResponse>)method.Invoke(null, [request, new TestLambdaContext()])!;
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
