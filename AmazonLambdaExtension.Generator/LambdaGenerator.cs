namespace AmazonLambdaExtension.Generator;

using AmazonLambdaExtension.Generator.Models;
using AmazonLambdaExtension.Generator.Templates;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

using System.Collections.Immutable;
using System.Text;

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

    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

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

    // TODO model
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

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        var builder = new SourceBuilder();
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

                // TODO code base
                builder.Clear();
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
                    BuildEventSource(builder, function, handler);
                    var source = builder.ToString();
                    context.AddSource($"{handler.WrapperClass}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }
    }

    private static void BuildEventSource(SourceBuilder builder, FunctionModel function, HandlerModel handler)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // namespace
        if (!String.IsNullOrEmpty(handler.Namespace))
        {
            builder.Namespace(handler.Namespace).NewLine();
        }

        builder.Using("Microsoft.Extensions.DependencyInjection").NewLine();

        // class
        builder
            .Append("public sealed class  ")
            .Append(handler.WrapperClass)
            .NewLine();
        builder.BeginScope();

        // member
        if (function.ServiceResolver is not null)
        {
            builder
                .Indent()
                .Append("private readonly ")
                .Append(function.ServiceResolver.FullName)
                .Append(" serviceResolver;")
                .NewLine();
            builder.NewLine();
        }

        if (function.Filter is not null)
        {
            builder
                .Indent()
                .Append("private readonly ")
                .Append(function.Filter.Type.FullName)
                .Append(" filter;")
                .NewLine();
            builder.NewLine();
        }

        builder
            .Indent()
            .Append("private readonly ")
            .Append(function.Function.FullName)
            .Append(" function;")
            .NewLine();
        builder.NewLine();

        // constructor
        builder
            .Indent()
            .Append("public ")
            .Append(handler.WrapperClass)
            .Append("()")
            .NewLine();
        builder.BeginScope();

        if (function.ServiceResolver is not null)
        {
            builder
                .Indent()
                .Append("serviceResolver = new ")
                .Append(function.ServiceResolver.FullName)
                .Append("();")
                .NewLine();
        }

        if (function.Filter is not null)
        {
            builder
                .Indent()
                .Append("filter = new ")
                .Append(function.Filter.Type.FullName)
                .Append("();")
                .NewLine();
        }

        if (function.ResolveFunction)
        {
            builder
                .Indent()
                .Append("function = serviceResolver.GetService<")
                .Append(function.Function.FullName)
                .Append(">()")
                .NewLine();
        }
        else
        {
            builder
                .Indent()
                .Append("function = new ")
                .Append(function.Function.FullName)
                .Append('(')
                .Append(String.Join(", ", function.ConstructorParameters.Select(static x => $"serviceResolver.GetService<{x.FullName}>()")))
                .Append(");")
                .NewLine();
        }

        builder.EndScope();
        builder.NewLine();

        // Method
        var contextIndex = handler.Parameters.FindIndex(static x => x.Type.IsLambdaContext());
        var arguments = handler.Parameters
            .Select(static (x, i) => new { Parameter = x, Index = i })
            .Where(static x => x.Parameter.ParameterType != ParameterType.FromServices)
            .Select(static x => $"{x.Parameter.Type.FullName} p{x.Index}");
        if (contextIndex < 0)
        {
            arguments = arguments.Append($"Amazon.Lambda.Core.ILambdaContext p{handler.Parameters.Count}");
        }
        var argument = String.Join(", ", arguments);
        var contextArgument = contextIndex < 0 ? $"p{handler.Parameters.Count}" : $"p{contextIndex}";

        if (handler.IsAsync || function.IsAsyncRequired())
        {
            if (handler.ResultType is not null)
            {
                builder
                    .Indent()
                    .Append("public async System.Threading.Tasks.Task<")
                    .Append(handler.ResultType.FullName)
                    .Append("> Handle(")
                    .Append(argument)
                    .Append(')')
                    .NewLine();
            }
            else
            {
                builder
                    .Indent()
                    .Append("public async System.Threading.Tasks.Task Handle(")
                    .Append(argument)
                    .Append(')')
                    .NewLine();
            }
        }
        else
        {
            if (handler.ResultType is not null)
            {
                builder
                    .Indent()
                    .Append("public ")
                    .Append(handler.ResultType.FullName)
                    .Append(" Handle(")
                    .Append(argument)
                    .Append(')')
                    .NewLine();
            }
            else
            {
                builder
                    .Indent()
                    .Append("public void Handle(")
                    .Append(argument)
                    .Append(')')
                    .NewLine();
            }
        }

        builder.BeginScope();

        // pre filter
        if (function.Filter?.Executing is not null)
        {
            builder
                .Indent()
                .AppendIf(function.Filter.Executing.IsAsync, "await ")
                .Append("filter.OnFunctionExecuting(")
                .Append(contextArgument)
                .Append(')')
                .AppendIf(function.Filter.Executing.IsAsync, ".ConfigureAwait(false)")
                .Append(';')
                .NewLine();
        }

        // try
        builder
            .Indent()
            .Append("try")
            .NewLine();
        builder.BeginScope();

        // parameters
        for (var i = 0; i < handler.Parameters.Count; i++)
        {
            var parameter = handler.Parameters[i];
            if (parameter.ParameterType == ParameterType.FromServices)
            {
                builder
                    .Indent()
                    .Append("var p")
                    .Append(i)
                    .Append(" = serviceResolver.GetService<")
                    .Append(parameter.Type.FullName)
                    .Append(">();")
                    .NewLine();
            }
        }

        // invoke
        builder
            .Indent()
            .AppendIf(handler.ResultType is not null, "return ")
            .AppendIf(handler.IsAsync, "await ")
            .Append("function.")
            .Append(handler.MethodName)
            .Append('(')
            .Append(String.Join(", ", handler.Parameters.Select(static (_, i) => $"p{i}")))
            .Append(')')
            .AppendIf(handler.IsAsync, ".ConfigureAwait(false)")
            .Append(';')
            .NewLine();

        builder.EndScope();

        // catch
        builder
            .Indent()
            .Append("catch (System.Exception ex)")
            .NewLine();
        builder.BeginScope();

        builder
            .Indent()
            .Append(contextArgument)
            .Append(".Logger.LogLine(ex.ToString());")
            .NewLine();
        builder
            .Indent()
            .Append("throw;")
            .NewLine();

        builder.EndScope();

        // post filter
        if (function.Filter?.Executed is not null)
        {
            builder
                .Indent()
                .Append("finally")
                .NewLine();
            builder.BeginScope();
            builder
                .Indent()
                .AppendIf(function.Filter.Executed.IsAsync, "await ")
                .Append("filter.OnFunctionExecuted(")
                .Append(contextArgument)
                .Append(')')
                .AppendIf(function.Filter.Executed.IsAsync, ".ConfigureAwait(false)")
                .Append(';')
                .NewLine();
            builder.EndScope();
        }

        builder.EndScope();

        builder.EndScope();
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
