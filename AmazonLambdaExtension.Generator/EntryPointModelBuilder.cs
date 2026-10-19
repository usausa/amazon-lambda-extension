namespace AmazonLambdaExtension.Generator;

using AmazonLambdaExtension.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using SourceGenerateHelper;

internal static class EntryPointModelBuilder
{
    // ReSharper disable InconsistentNaming
    private const string LambdaBootstrapBuilderFullName = "Amazon.Lambda.RuntimeSupport.LambdaBootstrapBuilder";
    private const string LambdaSerializerAttributeName = "Amazon.Lambda.Core.LambdaSerializerAttribute";
    private const string ILambdaSerializerFullName = "Amazon.Lambda.Core.ILambdaSerializer";
    // ReSharper restore InconsistentNaming

    public static Result<EntryPointModel> BuildEntryPointModel(GeneratorAttributeSyntaxContext context, AnalyzerConfigOptions options)
    {
        // フェーズ1: 対象アセンブリと診断バッファを初期化
        // Phase 1: Initialize the target assembly and the diagnostic buffer
        var compilation = context.SemanticModel.Compilation;
        var assemblyName = compilation.AssemblyName ?? string.Empty;
        var location = context.Attributes.Length > 0
            ? context.Attributes[0].ApplicationSyntaxReference?.GetSyntax().GetLocation()
            : null;
        var diagnostics = new List<DiagnosticInfo>();

        // フェーズ2: 生成する Main が呼ぶ LambdaBootstrapBuilder を参照できるかを検証
        // Phase 2: Validate that LambdaBootstrapBuilder, which the generated Main calls, is referenced
        if (compilation.GetTypeByMetadataName(LambdaBootstrapBuilderFullName) is null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.MissingRuntimeSupport, location, assemblyName));
        }

        // フェーズ2b: Native AOT で発行するときは、実行ファイルでないと bootstrap ができない（ライブラリのままだと、警告なしにネイティブのライブラリになる）
        // Phase 2b: When publishing with Native AOT, only an executable produces the bootstrap (a library silently becomes a native library)
        if (options.TryGetValue<bool>("PublishAot", out var publishAot) && publishAot &&
            (compilation.Options.OutputKind is not (OutputKind.ConsoleApplication or OutputKind.WindowsApplication)))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.EntryPointNotExecutable, location, assemblyName));
        }

        // フェーズ3: アセンブリの [LambdaSerializer] からランタイムのシリアライザー型を取り出す
        // Phase 3: Extract the runtime serializer type from the assembly-level [LambdaSerializer]
        var serializerAttr = compilation.Assembly.FindAttribute(LambdaSerializerAttributeName);
        if (serializerAttr is null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.MissingLambdaSerializer, location, assemblyName));
            return Results.Errors<EntryPointModel>(diagnostics);
        }

        // フェーズ4: Main で new するため、具象型・ILambdaSerializer 実装・到達可能な引数なしコンストラクタを検証
        // Phase 4: Main news the serializer, so validate a concrete type, ILambdaSerializer, and an accessible parameterless constructor
        var serializerType = (serializerAttr.ConstructorArguments.Length > 0) ? serializerAttr.ConstructorArguments[0].Value as INamedTypeSymbol : null;
        if ((serializerType is null) || !IsUsableSerializerType(serializerType, compilation))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.InvalidLambdaSerializer,
                location,
                serializerType?.ToDisplayString() ?? string.Empty));
        }

        // フェーズ5: エラーがなければ EntryPointModel を組み立てる
        // Phase 5: Build the EntryPointModel when no errors remain
        if (diagnostics.Count > 0)
        {
            return Results.Errors<EntryPointModel>(diagnostics);
        }

        return Results.Success(new EntryPointModel(
            assemblyName,
            serializerType!.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable)));
    }

    private static bool IsUsableSerializerType(INamedTypeSymbol type, Compilation compilation)
    {
        // オープンジェネリック・抽象型は new できず、ILambdaSerializer でなければランタイムに渡せない
        // Open generic and abstract types cannot be new'd, and the runtime only accepts an ILambdaSerializer
        return !type.IsUnboundGenericType &&
               !type.IsAbstract &&
               (type.TypeKind == TypeKind.Class) &&
               type.AllInterfaces.Any(static i => i.HasFullyQualifiedMetadataName(ILambdaSerializerFullName)) &&
               type.InstanceConstructors.Any(c => (c.Parameters.Length == 0) && compilation.IsSymbolAccessibleWithin(c, compilation.Assembly));
    }
}
