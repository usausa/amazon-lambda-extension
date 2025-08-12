namespace AmazonLambdaExtension.Generator;

using System.Collections.Immutable;
using System.Text;

using AmazonLambdaExtension.Generator.Templates;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class LambdaGenerator : IIncrementalGenerator
{
    private const string LambdaAttributeName = "AmazonLambdaExtension.Annotations.LambdaAttribute";
    private const string ApiAttributeName = "AmazonLambdaExtension.Annotations.ApiAttribute";
    private const string EventAttributeName = "AmazonLambdaExtension.Annotations.EventAttribute";

    private enum HandlerType
    {
        None,
        Api,
        Event
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => IsTargetSyntax(s),
                static (ctx, _) => GetTargetSyntax(ctx))
            .SelectMany(static (x, _) => x is not null ? ImmutableArray.Create(x) : []);
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
            context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterImplementationSourceOutput(compilationAndClasses, static (spc, source) => Execute(spc, source.Item1, source.Item2));
    }

    private static bool IsTargetSyntax(SyntaxNode node) =>
        node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0;

    private static ClassDeclarationSyntax? GetTargetSyntax(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(static x => x.Attributes))
        {
            if ((context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol) &&
                (attributeSymbol.ContainingType.ToDisplayString() == LambdaAttributeName))
            {
                return classDeclarationSyntax;
            }
        }

        return null;
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
            var function = ModelBuilder.BuildFunctionInfo((ITypeSymbol)classSymbol);

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
                var methodSymbol = methodSemantic.GetDeclaredSymbol(methodDeclarationSyntax)!;
                var handlerType = ResolveHandlerType(methodSymbol);
                if (handlerType == HandlerType.None)
                {
                    continue;
                }

                var handler = ModelBuilder.BuildHandlerInfo((IMethodSymbol)methodSymbol);

                if (handlerType == HandlerType.Api)
                {
                    // Generate wrapper
                    var template = new ApiTemplate(function, handler);
                    var sourceText = template.TransformText();
                    context.AddSource($"{handler.WrapperClass}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
                else if (handlerType == HandlerType.Event)
                {
                    // Generate wrapper
                    var template = new EventTemplate(function, handler);
                    var sourceText = template.TransformText();
                    context.AddSource($"{handler.WrapperClass}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
                }
            }
        }
    }

    private static HandlerType ResolveHandlerType(ISymbol symbol)
    {
        foreach (var name in symbol.GetAttributes().Select(static attribute => attribute.AttributeClass!.ToDisplayString()))
        {
            if (name == ApiAttributeName)
            {
                return HandlerType.Api;
            }
            if (name == EventAttributeName)
            {
                return HandlerType.Event;
            }
        }

        return HandlerType.None;
    }

    //private const string AttributeName = "AmazonLambdaExtension.CustomMethodAttribute";

    //// ------------------------------------------------------------
    //// Initialize
    //// ------------------------------------------------------------

    //public void Initialize(IncrementalGeneratorInitializationContext context)
    //{
    //    var optionProvider = context.AnalyzerConfigOptionsProvider
    //        .Select(SelectOption);

    //    var methodProvider = context.SyntaxProvider
    //        .ForAttributeWithMetadataName(
    //            AttributeName,
    //            static (syntax, _) => IsMethodSyntax(syntax),
    //            static (context, _) => GetMethodModel(context))
    //        .Collect();

    //    context.RegisterImplementationSourceOutput(
    //        optionProvider.Combine(methodProvider),
    //        static (context, provider) => Execute(context, provider.Left, provider.Right));
    //}

    //// ------------------------------------------------------------
    //// Parser
    //// ------------------------------------------------------------

    //private static OptionModel SelectOption(AnalyzerConfigOptionsProvider provider, CancellationToken token)
    //{
    //    var value = provider.GlobalOptions.GetValue<string>("TemplateLibraryGeneratorValue");
    //    return new OptionModel(value);
    //}

    //private static bool IsMethodSyntax(SyntaxNode syntax) =>
    //    syntax is MethodDeclarationSyntax;

    //private static Result<MethodModel> GetMethodModel(GeneratorAttributeSyntaxContext context)
    //{
    //    var syntax = (MethodDeclarationSyntax)context.TargetNode;
    //    if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol symbol)
    //    {
    //        return Results.Error<MethodModel>(null);
    //    }

    //    // Validate method definition
    //    if (!symbol.IsStatic || !symbol.IsPartialDefinition)
    //    {
    //        return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, syntax.GetLocation(), symbol.Name));
    //    }

    //    // Validate parameter
    //    if (symbol.Parameters.Length != 0)
    //    {
    //        return Results.Error<MethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
    //    }

    //    var containingType = symbol.ContainingType;
    //    var ns = String.IsNullOrEmpty(containingType.ContainingNamespace.Name)
    //        ? string.Empty
    //        : containingType.ContainingNamespace.ToDisplayString();

    //    return Results.Success(new MethodModel(
    //        ns,
    //        containingType.GetClassName(),
    //        containingType.IsValueType,
    //        symbol.DeclaredAccessibility,
    //        symbol.Name));
    //}

    //// ------------------------------------------------------------
    //// Generator
    //// ------------------------------------------------------------

    //private static void Execute(SourceProductionContext context, OptionModel option, ImmutableArray<Result<MethodModel>> methods)
    //{
    //    foreach (var info in methods.SelectError())
    //    {
    //        context.ReportDiagnostic(info);
    //    }

    //    var builder = new SourceBuilder();
    //    foreach (var group in methods.SelectValue().GroupBy(static x => new { x.Namespace, x.ClassName }))
    //    {
    //        context.CancellationToken.ThrowIfCancellationRequested();

    //        builder.Clear();
    //        BuildSource(builder, option, group.ToList());

    //        var filename = MakeFilename(group.Key.Namespace, group.Key.ClassName);
    //        var source = builder.ToString();
    //        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    //    }
    //}

    //private static void BuildSource(SourceBuilder builder, OptionModel option, List<MethodModel> methods)
    //{
    //    var ns = methods[0].Namespace;
    //    var className = methods[0].ClassName;
    //    var isValueType = methods[0].IsValueType;

    //    builder.AutoGenerated();
    //    builder.EnableNullable();
    //    builder.NewLine();

    //    // namespace
    //    if (!String.IsNullOrEmpty(ns))
    //    {
    //        builder.Namespace(ns);
    //        builder.NewLine();
    //    }

    //    // class
    //    builder
    //        .Indent()
    //        .Append("partial ")
    //        .Append(isValueType ? "struct " : "class ")
    //        .Append(className)
    //        .NewLine();
    //    builder.BeginScope();

    //    builder.Indent().Append("// Option: ").Append(option.Value).NewLine();

    //    var first = true;
    //    foreach (var method in methods)
    //    {
    //        if (first)
    //        {
    //            first = false;
    //        }
    //        else
    //        {
    //            builder.NewLine();
    //        }

    //        // method
    //        builder
    //            .Indent()
    //            .Append(method.MethodAccessibility.ToText())
    //            .Append(" static partial void ")
    //            .Append(method.MethodName)
    //            .Append("()")
    //            .NewLine();
    //        builder.BeginScope();

    //        builder
    //            .Indent()
    //            .Append("Console.WriteLine(\"Hello world.\");")
    //            .NewLine();

    //        builder.EndScope();
    //    }

    //    builder.EndScope();
    //}

    //// ------------------------------------------------------------
    //// Helper
    //// ------------------------------------------------------------

    //private static string MakeFilename(string ns, string className)
    //{
    //    var buffer = new StringBuilder();

    //    if (!String.IsNullOrEmpty(ns))
    //    {
    //        buffer.Append(ns.Replace('.', '_'));
    //        buffer.Append('_');
    //    }

    //    buffer.Append(className.Replace('<', '[').Replace('>', ']'));
    //    buffer.Append(".g.cs");

    //    return buffer.ToString();
    //}
}
