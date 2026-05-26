namespace AmazonLambdaExtension;

using System.Reflection;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// テスト用のコンパイルヘルパー。
/// ソースコードを Roslyn でコンパイルし、LambdaGenerator を使って生成コードを取得する。
/// </summary>
public static class CompilationHelper
{
    /// <summary>
    /// ソースコードから LambdaGenerator を実行し、生成されたソースファイルを返す。
    /// キー: ヒント名（ファイル名）、値: 生成されたソースコード。
    /// </summary>
    public static IReadOnlyDictionary<string, string> RunGenerator(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new LambdaGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGenerators(compilation);

        var result = driver.GetRunResult();
        return result.GeneratedTrees
            .ToDictionary(
                t => System.IO.Path.GetFileName(t.FilePath),
                t => t.GetText().ToString());
    }

    /// <summary>
    /// ソースコードから Compilation を生成する（診断テスト用）。
    /// </summary>
    public static (Compilation Compilation, IReadOnlyList<Diagnostic> Diagnostics) RunGeneratorWithDiagnostics(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new LambdaGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGenerators(compilation);

        var result = driver.GetRunResult();
        var diagnostics = result.Results
            .SelectMany(static r => r.Diagnostics)
            .ToList();

        return (compilation, diagnostics);
    }

    /// <summary>
    /// ソースコードから Compilation を生成する。
    /// </summary>
    public static CSharpCompilation CreateCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            GetDefaultReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> GetDefaultReferences()
    {
        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(LambdaAttribute).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(APIGatewayHttpApiV2ProxyRequest).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(ILambdaContext).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(SQSEvent).GetTypeInfo().Assembly.Location);

        // System 関連アセンブリ
        var systemAssembly = typeof(object).Assembly;
        var runtimeDir = System.IO.Path.GetDirectoryName(systemAssembly.Location)!;
        foreach (var name in new[] { "System.Runtime.dll", "System.Collections.dll", "System.Threading.Tasks.dll", "netstandard.dll", "System.Linq.dll" })
        {
            var path = System.IO.Path.Combine(runtimeDir, name);
            if (System.IO.File.Exists(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }
}
