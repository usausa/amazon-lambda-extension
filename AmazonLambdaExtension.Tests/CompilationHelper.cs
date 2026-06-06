namespace AmazonLambdaExtension;

using System.Collections.Generic;
using System.Reflection;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using AmazonLambdaExtension.Annotations;
using AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// テスト用のコンパイルヘルパー。
// ソースコードを Roslyn でコンパイルし、LambdaGenerator を使って生成コードを取得する。
public static class CompilationHelper
{
    // ソースコードから LambdaGenerator を実行し、生成されたソースファイルを返す。
    // キー: ヒント名（ファイル名）、値: 生成されたソースコード。
    public static IReadOnlyDictionary<string, string> RunGenerator(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new LambdaGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator.AsSourceGenerator())
            .RunGenerators(compilation);

        var result = driver.GetRunResult();
        return result.GeneratedTrees
            .ToDictionary(
                t => Path.GetFileName(t.FilePath),
                t => t.GetText().ToString());
    }

    // ソースコードに対して LambdaGenerator を実行し、生成時の診断のみを返す（診断テスト用）。
    public static IReadOnlyList<Diagnostic> RunGeneratorWithDiagnostics(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new LambdaGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator.AsSourceGenerator())
            .RunGenerators(compilation);

        var result = driver.GetRunResult();
        return result.Results
            .SelectMany(static r => r.Diagnostics)
            .ToList();
    }

    // ソースコードから Compilation を生成する。
    private static CSharpCompilation CreateCompilation(string source)
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
        // ライブラリ参照
        yield return MetadataReference.CreateFromFile(typeof(LambdaAttribute).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(APIGatewayHttpApiV2ProxyRequest).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(ILambdaContext).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(SQSEvent).GetTypeInfo().Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).GetTypeInfo().Assembly.Location);

        // System 関連アセンブリ（AppContext.GetData で信頼できるパスから取得）
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedPlatformAssemblies is not null)
        {
            var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Runtime.dll",
                "System.Collections.dll",
                "System.Threading.Tasks.dll",
                "System.Linq.dll",
                "System.Text.RegularExpressions.dll",
                "System.ObjectModel.dll",
                "System.Console.dll",
                "System.ComponentModel.dll",
                "System.Net.Primitives.dll",
                "netstandard.dll",
                "mscorlib.dll"
            };

            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                var fileName = Path.GetFileName(path);
                if (needed.Contains(fileName) || fileName.StartsWith("System.Private.", StringComparison.Ordinal))
                {
                    yield return MetadataReference.CreateFromFile(path);
                }
            }
        }
        else
        {
            // フォールバック: ランタイムディレクトリから取得
            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            foreach (var name in new[] { "System.Runtime.dll", "System.Collections.dll", "System.Threading.Tasks.dll", "netstandard.dll", "System.Linq.dll" })
            {
                var path = Path.Combine(runtimeDir, name);
                if (File.Exists(path))
                {
                    yield return MetadataReference.CreateFromFile(path);
                }
            }
        }
    }
}
