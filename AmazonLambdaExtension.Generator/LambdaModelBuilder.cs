#pragma warning disable IDE0060, IDE0042, SA1313
namespace AmazonLambdaExtension.Generator;

using AmazonLambdaExtension.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SourceGenerateHelper;

internal static class LambdaModelBuilder
{
    // ReSharper disable InconsistentNaming
    private const string ServiceResolverAttributeName = "AmazonLambdaExtension.Annotations.ServiceResolverAttribute";
    private const string FilterAttributeName = "AmazonLambdaExtension.Annotations.FilterAttribute`1";
    private const string HttpApiAttributeName = "AmazonLambdaExtension.Annotations.HttpApiAttribute";
    private const string FunctionUrlAttributeName = "AmazonLambdaExtension.Annotations.FunctionUrlAttribute";
    private const string HttpApiAuthorizerAttributeName = "AmazonLambdaExtension.Annotations.HttpApiAuthorizerAttribute";
    private const string EventAttributeName = "AmazonLambdaExtension.Annotations.EventAttribute";

    private const string FromBodyAttributeName = "AmazonLambdaExtension.Annotations.FromBodyAttribute";
    private const string FromQueryAttributeName = "AmazonLambdaExtension.Annotations.FromQueryAttribute";
    private const string FromHeaderAttributeName = "AmazonLambdaExtension.Annotations.FromHeaderAttribute";
    private const string FromRouteAttributeName = "AmazonLambdaExtension.Annotations.FromRouteAttribute";
    private const string FromServicesAttributeName = "AmazonLambdaExtension.Annotations.FromServicesAttribute";
    private const string FromCustomAuthorizerAttributeName = "AmazonLambdaExtension.Annotations.FromCustomAuthorizerAttribute";

    private const string IHttpResultFullName = "AmazonLambdaExtension.APIGateway.IHttpResult";
    private const string IAuthorizerResultFullName = "AmazonLambdaExtension.APIGateway.IAuthorizerResult";
    private const string ILambdaFilterFullName = "AmazonLambdaExtension.Filters.ILambdaFilter";
    private const string IServiceCollectionFullName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string HttpApiRequestFullName = "Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest";
    private const string HttpApiAuthorizerRequestFullName = "Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerV2Request";
    // resultType.FullName は完全修飾形式（global:: 付き）で生成されるため、それに合わせる
    // resultType.FullName is produced in fully-qualified form (with global::), so match that form
    private const string HttpApiResponseFullName = "global::Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse";
    private const string LambdaContextFullName = "Amazon.Lambda.Core.ILambdaContext";
    // ReSharper restore InconsistentNaming

    public static Result<LambdaModel> BuildLambdaModel(GeneratorAttributeSyntaxContext context)
    {
        // フェーズ1: 対象クラスと診断バッファを初期化
        // Phase 1: Initialize the target class context and the diagnostic buffer
        var syntax = (TypeDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        var compilation = context.SemanticModel.Compilation;
        var diagnostics = new List<DiagnosticInfo>();

        // フェーズ2: ジェネレーターが差し込める構造（partial・非ジェネリック・トップレベル）かを最初に検証
        // Phase 2: Validate that the target is a structure (partial, non-generic, top-level) that can receive generated members
        var isPartial = syntax.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
        if (!isPartial)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.NotPartialClass, syntax.GetLocation(), symbol.Name));
        }

        // ジェネリック型は型パラメータを生成側で再現できないため未対応
        // Generic types are unsupported because the generator cannot reproduce their type parameters
        if (symbol.TypeParameters.Length > 0)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.GenericLambdaClass, syntax.GetLocation(), symbol.Name));
        }

        // ネストされた型は外側型の入れ子構造を生成側で再現できないため未対応
        // Nested types are unsupported because the generator cannot reproduce the enclosing type nesting
        if (symbol.ContainingType is not null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.NestedLambdaClass, syntax.GetLocation(), symbol.Name));
        }

        // record（record class）は宣言形を生成側で再現できないため未対応
        // （struct / record struct は [Lambda] の AttributeTargets.Class によりコンパイラが弾くため到達しない）
        // Records (record class) are unsupported because the generator cannot reproduce their declaration form
        // (struct / record struct never reach here because [Lambda] is restricted to AttributeTargets.Class)
        if (symbol.IsRecord)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.RecordLambdaClass, syntax.GetLocation(), symbol.Name));
        }

        // abstract クラスは new FunctionType(...) でインスタンス化できない（本体は DI 有無に関わらず常に new 生成）ため未対応
        // An abstract class cannot be instantiated via new FunctionType(...) (the target is always new'd regardless of DI), so it is unsupported
        if (symbol.IsAbstract)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.AbstractLambdaClass, syntax.GetLocation(), symbol.Name));
        }

        if (HasErrors(diagnostics))
        {
            return Results.Errors<LambdaModel>(diagnostics.ToArray());
        }

        // フェーズ3: 名前空間、型参照、コンストラクタ依存など基礎メタデータを抽出
        // Phase 3: Extract core metadata such as namespace, type references, and constructor dependencies
        var ns = string.IsNullOrEmpty(symbol.ContainingNamespace.Name)
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var functionType = MakeTypeRef(symbol);

        var ctor = symbol.InstanceConstructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(static c => c.Parameters.Length)
            .FirstOrDefault();

        var ctorParams = ctor?.Parameters.Select(static p => MakeTypeRef(p.Type)).ToArray() ?? [];

        // フェーズ4: [ServiceResolver] と ConfigureServices() 契約を検証
        // Phase 4: Validate [ServiceResolver] usage and the ConfigureServices() contract
        TypeRefModel? serviceResolver = null;
        var serviceResolverAttr = symbol.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == ServiceResolverAttributeName);
        if (serviceResolverAttr is not null &&
            serviceResolverAttr.ConstructorArguments.Length > 0 &&
            serviceResolverAttr.ConstructorArguments[0].Value is INamedTypeSymbol resolverType)
        {
            // ConfigureServices は Lambda クラス内（BuildProvider）から呼ぶため、public 固定ではなく
            // Lambda クラスから到達可能な static メソッドであればよい（同一アセンブリ internal 等）
            // ConfigureServices is called from inside the Lambda class (BuildProvider), so it need not be public;
            // a static method accessible from the Lambda class is sufficient (e.g. same-assembly internal)
            var configureMethod = resolverType.GetMembers("ConfigureServices")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.IsStatic
                    && m.Parameters.Length == 0
                    && m.ReturnType.ToDisplayString() == IServiceCollectionFullName
                    && compilation.IsSymbolAccessibleWithin(m, symbol));

            if (configureMethod is null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.InvalidServiceResolverType,
                    syntax.GetLocation(),
                    resolverType.ToDisplayString()));
            }
            else
            {
                serviceResolver = MakeTypeRef(resolverType);
            }
        }
        else if (ctorParams.Length > 0)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.MissingServiceResolver,
                syntax.GetLocation(),
                symbol.Name));
        }

        // [ServiceResolver] が無い場合、生成コードは（同一 partial クラス内で）new FunctionType() を出力するため
        // Lambda クラスから到達可能な parameterless ctor が必須（in-class 生成なので private でも可）。
        // ctor 引数ありは ALE0007 で扱うため ctorParams.Length == 0 に限定して二重診断を避ける。
        // Without [ServiceResolver] the generated code emits new FunctionType() (inside the same partial class),
        // so a parameterless constructor accessible from the Lambda class is required (private is fine in-class).
        // Limited to ctorParams.Length == 0 so ALE0007 covers the public-ctor-with-parameters case without duplication.
        if (serviceResolverAttr is null &&
            ctorParams.Length == 0 &&
            !HasAccessibleParameterlessConstructor(symbol, symbol, compilation))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.LambdaClassNoParameterlessCtor,
                syntax.GetLocation(),
                symbol.Name));
        }

        // フェーズ5: クラスレベルの [Filter<T>] を順序付きで収集し、型契約を検証
        // Phase 5: Collect ordered class-level [Filter<T>] declarations and validate their type contract
        var sortedFilters = symbol.GetAttributes()
            .Select(static (a, i) => (Attribute: a, AttributeIndex: i))
            .Where(static x => IsFilterAttribute(x.Attribute))
            .OrderBy(static x => GetFilterOrder(x.Attribute))
            .ThenBy(static x => x.AttributeIndex)
            .Select(static x => (
                x.Attribute,
                FilterType: MakeTypeRef(x.Attribute.AttributeClass!.TypeArguments[0])))
            .ToArray();

        foreach (var filter in sortedFilters)
        {
            var filterTypeArg = filter.Attribute.AttributeClass!.TypeArguments[0];
            if (filterTypeArg is not INamedTypeSymbol filterTypeSym)
            {
                continue;
            }

            if (!ImplementsInterface(filterTypeSym, ILambdaFilterFullName))
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.FilterNotImplementILambdaFilter,
                    syntax.GetLocation(),
                    filter.FilterType.FullName));
            }

            // [ServiceResolver] が無い場合、生成コードは Lambda クラス内で new FilterType() を出力する
            // Without [ServiceResolver] the generated code emits new FilterType() inside the Lambda class
            if (serviceResolverAttr is null)
            {
                if (filterTypeSym.IsAbstract)
                {
                    // abstract は public ctor があっても new できない（CS0144）ため別途診断
                    // An abstract type cannot be new'd (CS0144) even with a public ctor, so diagnose separately
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.AbstractFilter,
                        syntax.GetLocation(),
                        filter.FilterType.FullName));
                }
                else if (!HasAccessibleParameterlessConstructor(filterTypeSym, symbol, compilation))
                {
                    // Lambda クラスから到達可能な parameterless ctor が必須（public / 同一アセンブリ internal / nested 等）
                    // A parameterless constructor accessible from the Lambda class is required
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.FilterNoParameterlessCtor,
                        syntax.GetLocation(),
                        filter.FilterType.FullName));
                }
            }
        }

        // フェーズ6: メソッドごとにハンドラーモデルを構築し、個別診断を集約
        // Phase 6: Build handler models method by method and aggregate their diagnostics
        var handlers = new List<HandlerModel>();
        var handlerMethods = new List<IMethodSymbol>();
        foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary || member.IsStatic)
            {
                continue;
            }

            var (handlerModel, handlerDiagnostics) = BuildHandlerModel(symbol, member);
            diagnostics.AddRange(handlerDiagnostics);
            if (handlerModel is not null)
            {
                handlers.Add(handlerModel);
                handlerMethods.Add(member);
            }
        }

        // フェーズ7: 収集済みハンドラー一覧に基づくクラス単位の後続制約を検証
        // Phase 7: Run follow-up class-level validation based on the collected handlers
        if (serviceResolver is null &&
            handlers.Any(static h => h.Parameters.Any(static p => p.BindingType == ParameterBindingType.FromServices)))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.MissingServiceResolverForFromServices,
                syntax.GetLocation(),
                symbol.Name));
        }

        // 同名ハンドラー（オーバーロード）は生成名・hint name が衝突するため診断で停止する
        // Overloaded handlers collide in generated method names and hint names, so stop with a diagnostic
        // ReSharper disable LoopCanBeConvertedToQuery
        foreach (var overloadGroup in handlerMethods.GroupBy(static m => m.Name).Where(static g => g.Count() > 1))
        {
            foreach (var overload in overloadGroup)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.OverloadedHandler, GetLocation(overload), overload.Name));
            }
        }
        // ReSharper restore LoopCanBeConvertedToQuery

        // フェーズ8: エラーがなければ LambdaModel を組み立て、warning を添えて返す
        // Phase 8: Build the LambdaModel when no errors remain and return it with warnings
        if (HasErrors(diagnostics))
        {
            return Results.Errors<LambdaModel>(diagnostics.ToArray());
        }

        var model = new LambdaModel(
            ns,
            symbol.Name,
            functionType,
            new EquatableArray<TypeRefModel>(ctorParams),
            serviceResolver,
            new EquatableArray<TypeRefModel>(sortedFilters.Select(static x => x.FilterType).ToArray()),
            new EquatableArray<HandlerModel>(handlers.ToArray()));

        return new Result<LambdaModel>(model, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static (HandlerModel? Model, IReadOnlyList<DiagnosticInfo> Diagnostics) BuildHandlerModel(
        INamedTypeSymbol containingType,
        IMethodSymbol method)
    {
        // フェーズ1: ハンドラー単位の診断収集を開始
        // Phase 1: Start collecting diagnostics for a single handler method
        var diagnostics = new List<DiagnosticInfo>();

        // フェーズ2: ハンドラー属性を走査して種別と付随設定を決定
        // Phase 2: Scan handler attributes to determine its kind and options
        HandlerType? handlerType = null;
        var enableSimpleResponses = true;
        string? authorizerMethodName = null;
        var handlerAttrCount = 0;

        foreach (var attr in method.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName == HttpApiAttributeName)
            {
                handlerAttrCount++;
                handlerType = HandlerType.HttpApi;
                authorizerMethodName = GetNamedStringArgument(attr, "Authorizer");
            }
            else if (attrName == FunctionUrlAttributeName)
            {
                handlerAttrCount++;
                handlerType = HandlerType.FunctionUrl;
            }
            else if (attrName == HttpApiAuthorizerAttributeName)
            {
                handlerAttrCount++;
                handlerType = HandlerType.HttpApiAuthorizer;
                var enableSimple = attr.NamedArguments.FirstOrDefault(static a => a.Key == "EnableSimpleResponses").Value.Value;
                enableSimpleResponses = enableSimple is not false;
            }
            else if (attrName == EventAttributeName)
            {
                handlerAttrCount++;
                handlerType = HandlerType.Event;
            }
        }

        if (handlerType is null)
        {
            if (method.DeclaredAccessibility == Accessibility.Public)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.NoHandlerAttribute,
                    GetLocation(method),
                    method.Name));
            }

            return (null, diagnostics);
        }

        // フェーズ3: ハンドラー属性の競合と authorizer 参照を検証
        // Phase 3: Validate duplicate handler attributes and authorizer references
        if (handlerAttrCount > 1)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.MultipleHandlerAttributes, GetLocation(method), method.Name));
        }

        if (handlerType == HandlerType.HttpApi &&
            !string.IsNullOrWhiteSpace(authorizerMethodName) &&
            !HasAuthorizerMethod(containingType, authorizerMethodName!))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.AuthorizerMethodNotFound,
                GetLocation(method),
                authorizerMethodName!));
        }

        // フェーズ4: パラメータごとの binding モデルと診断を構築
        // Phase 4: Build parameter binding models and aggregate their diagnostics
        var parameters = new List<ParameterModel>();
        foreach (var param in method.Parameters)
        {
            var (parameterModel, parameterDiagnostics) = BuildParameterModel(method, param, handlerType.Value);
            diagnostics.AddRange(parameterDiagnostics);
            if (parameterModel is not null)
            {
                parameters.Add(parameterModel);
            }
        }

        // フェーズ4b: Event ハンドラーは payload（Request 扱いの引数）をちょうど 1 件に制限する
        // Phase 4b: Event handlers must declare exactly one event payload (Request-bound) parameter
        // パラメータ単位の診断が出ているときはエラーの連鎖を避けるため検証しない
        // Skip this check when parameter-level diagnostics already exist to avoid cascading errors
        if (handlerType == HandlerType.Event && !HasErrors(diagnostics))
        {
            var payloadCount = parameters.Count(static p => p.BindingType == ParameterBindingType.Request);
            if (payloadCount == 0)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.EventHandlerMissingPayload, GetLocation(method), method.Name));
            }
            else if (payloadCount > 1)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.EventHandlerMultiplePayloads, GetLocation(method), method.Name));
            }
        }

        // フェーズ5: 戻り値型から async 性と実際の結果型を正規化
        // Phase 5: Normalize async behavior and the effective result type from the return type
        var returnType = method.ReturnType;
        TypeRefModel? resultType;
        var isAsync = false;

        if (returnType is INamedTypeSymbol namedReturn)
        {
            if (namedReturn.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>" ||
                namedReturn.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>")
            {
                isAsync = true;
                resultType = MakeTypeRef(namedReturn.TypeArguments[0]);
            }
            else if (namedReturn.ToDisplayString() == "System.Threading.Tasks.Task" ||
                     namedReturn.ToDisplayString() == "System.Threading.Tasks.ValueTask")
            {
                isAsync = true;
                resultType = null;
            }
            else if (namedReturn.ToDisplayString() == "void")
            {
                resultType = null;
            }
            else
            {
                resultType = MakeTypeRef(namedReturn);
            }
        }
        else
        {
            resultType = MakeTypeRef(returnType);
        }

        var returnsHttpResult = resultType is not null && IsImplementing(method.ReturnType, IHttpResultFullName);
        var returnsProxyResponse = resultType is not null && resultType.FullName == HttpApiResponseFullName;

        // フェーズ6: ハンドラー種別ごとの戻り値制約を検証
        // Phase 6: Validate return-type constraints that depend on the handler kind
        if (handlerType == HandlerType.HttpApiAuthorizer &&
            !(resultType is not null && IsImplementing(method.ReturnType, IAuthorizerResultFullName)))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.AuthorizerInvalidReturnType, GetLocation(method), method.Name));
        }

        if (HasErrors(diagnostics))
        {
            return (null, diagnostics);
        }

        // フェーズ7: 検証済みメタデータから HandlerModel を構築
        // Phase 7: Build the HandlerModel from the validated metadata
        return (
            new HandlerModel(
                method.Name,
                handlerType.Value,
                isAsync,
                resultType,
                returnsHttpResult,
                returnsProxyResponse,
                new EquatableArray<ParameterModel>(parameters.ToArray()),
                enableSimpleResponses),
            diagnostics);
    }

    private static (ParameterModel? Model, IReadOnlyList<DiagnosticInfo> Diagnostics) BuildParameterModel(
        IMethodSymbol method,
        IParameterSymbol param,
        HandlerType handlerType)
    {
        // フェーズ1: binding 属性を収集し、パラメータ単位の診断を開始
        // Phase 1: Collect binding attributes and start parameter-level diagnostics
        var diagnostics = new List<DiagnosticInfo>();
        var bindingAttributes = param.GetAttributes().Where(HasBindingAttribute).ToArray();

        if (bindingAttributes.Length > 1)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.MultipleBindingAttributes,
                GetLocation(param),
                method.Name,
                param.Name));
        }

        // フェーズ2: 明示属性または暗黙ルールから binding 種別を決定
        // Phase 2: Determine the binding kind from explicit attributes or implicit conventions
        var explicitBinding = bindingAttributes.FirstOrDefault();
        var bindingType = ParameterBindingType.FromQuery;
        var key = param.Name;
        var converterMethod = GetConverterMethod(param.Type);

        if (explicitBinding is not null)
        {
            ApplyExplicitBinding(explicitBinding, ref bindingType, ref key);
        }
        else
        {
            var typeName = param.Type.ToDisplayString();
            if ((typeName == HttpApiRequestFullName) || (typeName == HttpApiAuthorizerRequestFullName))
            {
                bindingType = ParameterBindingType.Request;
                converterMethod = string.Empty;
            }
            else if (typeName == LambdaContextFullName)
            {
                bindingType = ParameterBindingType.Context;
                converterMethod = string.Empty;
            }
            else if (handlerType == HandlerType.Event)
            {
                bindingType = ParameterBindingType.Request;
                converterMethod = string.Empty;
            }
        }

        // フェーズ3: ハンドラー種別ごとの binding 制約を検証
        // Phase 3: Validate binding restrictions that depend on the handler kind
        if (handlerType == HandlerType.Event && explicitBinding is not null)
        {
            var explicitBindingName = explicitBinding.AttributeClass?.ToDisplayString();
            if (explicitBindingName == FromBodyAttributeName)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.FromBodyOnEventHandler, GetLocation(method), method.Name));
            }
            else if (bindingType != ParameterBindingType.FromServices)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.InvalidEventBinding,
                    GetLocation(param),
                    method.Name,
                    GetAttributeDisplayName(explicitBinding),
                    param.Name));
            }
        }

        if (bindingType == ParameterBindingType.FromCustomAuthorizer &&
            handlerType != HandlerType.HttpApi)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.FromCustomAuthorizerOutsideHttpApi,
                GetLocation(param),
                method.Name));
        }

        if (RequiresScalarBindingValidation(bindingType) &&
            !IsSupportedBindingType(param.Type))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.UnsupportedBindingType,
                GetLocation(param),
                param.Type.ToDisplayString()));
        }

        // フェーズ4: [FromBody] の SkipValidate 指定を抽出
        // Phase 4: Extract the SkipValidate option from [FromBody]
        var skipValidation = false;
        var fromBodyAttr = param.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == FromBodyAttributeName);
        if (fromBodyAttr is not null)
        {
            var skipArg = fromBodyAttr.NamedArguments.FirstOrDefault(static a => a.Key == "SkipValidate").Value.Value;
            skipValidation = skipArg is true;
        }

        if (HasErrors(diagnostics))
        {
            return (null, diagnostics);
        }

        // フェーズ5: 検証済み binding 情報から ParameterModel を構築
        // Phase 5: Build the ParameterModel from the validated binding metadata
        return (
            new ParameterModel(
                param.Name,
                MakeTypeRef(param.Type),
                bindingType,
                key,
                converterMethod,
                skipValidation),
            diagnostics);
    }

    private static void ApplyExplicitBinding(AttributeData attr, ref ParameterBindingType bindingType, ref string key)
    {
        // 明示的な binding 属性を ParameterBindingType とキー名へ正規化
        // Normalize an explicit binding attribute into a ParameterBindingType and key name
        var attrName = attr.AttributeClass?.ToDisplayString();
        if (attrName == FromBodyAttributeName)
        {
            bindingType = ParameterBindingType.FromBody;
            return;
        }

        if (attrName == FromQueryAttributeName)
        {
            bindingType = ParameterBindingType.FromQuery;
            ApplyKeyOverride(attr, ref key);
            return;
        }

        if (attrName == FromHeaderAttributeName)
        {
            bindingType = ParameterBindingType.FromHeader;
            ApplyKeyOverride(attr, ref key);
            return;
        }

        if (attrName == FromRouteAttributeName)
        {
            bindingType = ParameterBindingType.FromRoute;
            ApplyKeyOverride(attr, ref key);
            return;
        }

        if (attrName == FromServicesAttributeName)
        {
            bindingType = ParameterBindingType.FromServices;
            // [FromServices] のキーは未指定時 empty とし、keyed service 解決の有無を区別する
            // For [FromServices] the key defaults to empty so keyed vs. non-keyed resolution can be distinguished
            key = string.Empty;
            ApplyKeyOverride(attr, ref key);
            return;
        }

        if (attrName == FromCustomAuthorizerAttributeName)
        {
            bindingType = ParameterBindingType.FromCustomAuthorizer;
            ApplyKeyOverride(attr, ref key);
        }
    }

    private static void ApplyKeyOverride(AttributeData attr, ref string key)
    {
        // [FromQuery("name")] のような属性引数からキー名の上書きを取り出す
        // Read a key override such as [FromQuery("name")] from the attribute constructor argument
        var nameArg = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
        if (!string.IsNullOrEmpty(nameArg))
        {
            key = nameArg!;
        }
    }

    private static bool HasAuthorizerMethod(INamedTypeSymbol containingType, string authorizerMethodName)
    {
        // HttpApiAttribute.Authorizer で指定された名前に対応する [HttpApiAuthorizer] メソッドを探す
        // Locate the [HttpApiAuthorizer] method referenced by HttpApiAttribute.Authorizer
        return containingType.GetMembers(authorizerMethodName)
            .OfType<IMethodSymbol>()
            .Any(static m => m.MethodKind == MethodKind.Ordinary &&
                             !m.IsStatic &&
                             m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == HttpApiAuthorizerAttributeName));
    }

    private static bool RequiresScalarBindingValidation(ParameterBindingType bindingType)
    {
        // 文字列入力から scalar 変換を行う binding 種別だけ型サポート検証の対象にする
        // Limit type-support validation to binding kinds that convert from scalar string inputs
        return bindingType == ParameterBindingType.FromQuery ||
               bindingType == ParameterBindingType.FromHeader ||
               bindingType == ParameterBindingType.FromRoute ||
               bindingType == ParameterBindingType.FromCustomAuthorizer;
    }

    private static bool IsSupportedBindingType(ITypeSymbol type)
    {
        // 配列 / Nullable<T> をほどきながら、scalar binding で扱える型かを判定
        // Unwrap arrays / Nullable<T> and determine whether scalar binding supports the type
        if (type is IArrayTypeSymbol arrayType)
        {
            return IsSupportedBindingType(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
        {
            return IsSupportedBindingType(namedType.TypeArguments[0]);
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        var fullName = type.ToDisplayString();
        if ((fullName == "string") || (fullName == "System.String"))
        {
            return true;
        }

        return !string.IsNullOrEmpty(GetConverterMethod(type));
    }

    private static bool HasErrors(IEnumerable<DiagnosticInfo> diagnostics)
    {
        // 収集済み診断の中に Error があるかだけを判定する
        // Check whether the collected diagnostics contain any errors
        return diagnostics.Any(static d => d.Descriptor.DefaultSeverity == DiagnosticSeverity.Error);
    }

    private static string? GetNamedStringArgument(AttributeData attr, string name)
    {
        // 属性の named argument から string 値を取り出す共通ヘルパー
        // Shared helper to read a string value from a named attribute argument
        var value = attr.NamedArguments.FirstOrDefault(a => a.Key == name).Value.Value;
        return value as string;
    }

    private static Location? GetLocation(ISymbol symbol)
    {
        // 診断位置は先頭 Location を優先して使う
        // Prefer the first symbol location when reporting diagnostics
        return symbol.Locations.Length > 0 ? symbol.Locations[0] : null;
    }

    private static string GetAttributeDisplayName(AttributeData attr)
    {
        // 診断メッセージ向けに Attribute 接尾辞を外した表示名を返す
        // Return a display name without the Attribute suffix for diagnostics
        var name = attr.AttributeClass?.Name ?? "Attribute";
        return name.EndsWith("Attribute", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "Attribute".Length)
            : name;
    }

    private static bool IsFilterAttribute(AttributeData attr)
    {
        // generic な [Filter<T>] だけをクラス属性の列挙から見分ける
        // Identify only generic [Filter<T>] attributes among class attributes
        var attrClass = attr.AttributeClass;
        if (attrClass is null)
        {
            return false;
        }

        if (attrClass.IsGenericType)
        {
            var original = attrClass.OriginalDefinition;
            var ns = original.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            return ns + "." + original.MetadataName == FilterAttributeName;
        }

        return false;
    }

    private static int GetFilterOrder(AttributeData attr)
    {
        // FilterAttribute.Order を読み取り、未指定時は 0 扱いにする
        // Read FilterAttribute.Order and default to 0 when it is omitted
        var namedArg = attr.NamedArguments.FirstOrDefault(static a => a.Key == "Order");
        if (namedArg.Value.Value is int order)
        {
            return order;
        }

        return 0;
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, string interfaceFullName)
    {
        // フィルター検証用に指定インターフェイス実装の有無を調べる
        // Check whether the type implements the specified interface for filter validation
        return type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceFullName);
    }

    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type, INamedTypeSymbol within, Compilation compilation)
    {
        // 生成箇所（within = Lambda クラス）から呼べる引数なしコンストラクタがあるかを判定
        // public 固定ではなく、実際に new できるか（同一アセンブリ internal や in-class private 等）で見る
        // Determine whether a parameterless constructor callable from the generation site (within = Lambda class) exists
        // Judged by actual constructability (same-assembly internal, in-class private, etc.), not a fixed public rule
        return type.InstanceConstructors.Any(c =>
            c.Parameters.Length == 0 && compilation.IsSymbolAccessibleWithin(c, within));
    }

    private static bool IsImplementing(ITypeSymbol type, string interfaceName)
    {
        // Task<T>/ValueTask<T> をほどきつつ、最終的な型が指定インターフェイスを実装するか判定
        // Unwrap Task<T>/ValueTask<T> and determine whether the effective type implements the interface
        if (type is INamedTypeSymbol named)
        {
            if (named.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>" ||
                named.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>")
            {
                return IsImplementing(named.TypeArguments[0], interfaceName);
            }

            return named.ToDisplayString() == interfaceName ||
                   named.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName);
        }

        return false;
    }

    private static bool HasBindingAttribute(AttributeData attr)
    {
        // パラメータ binding として扱う属性群かどうかをまとめて判定
        // Determine whether the attribute belongs to the supported parameter-binding set
        var name = attr.AttributeClass?.ToDisplayString();
        return name == FromBodyAttributeName || name == FromQueryAttributeName ||
               name == FromHeaderAttributeName || name == FromRouteAttributeName ||
               name == FromServicesAttributeName || name == FromCustomAuthorizerAttributeName;
    }

    private static string GetConverterMethod(ITypeSymbol type)
    {
        // 配列 / Nullable<T> をほどいた最終型に対して StringConverter のメソッド名を決める
        // Resolve the StringConverter method name for the final type after unwrapping arrays / Nullable<T>
        if (type is IArrayTypeSymbol arr)
        {
            return GetConverterMethod(arr.ElementType);
        }

        if (type is INamedTypeSymbol named && named.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
        {
            return GetConverterMethod(named.TypeArguments[0]);
        }

        var fullName = type.ToDisplayString();
        return fullName switch
        {
            "bool" or "System.Boolean" => "TryToBoolean",
            "byte" or "System.Byte" => "TryToByte",
            "sbyte" or "System.SByte" => "TryToSByte",
            "short" or "System.Int16" => "TryToInt16",
            "ushort" or "System.UInt16" => "TryToUInt16",
            "int" or "System.Int32" => "TryToInt32",
            "uint" or "System.UInt32" => "TryToUInt32",
            "long" or "System.Int64" => "TryToInt64",
            "ulong" or "System.UInt64" => "TryToUInt64",
            "float" or "System.Single" => "TryToSingle",
            "double" or "System.Double" => "TryToDouble",
            "decimal" or "System.Decimal" => "TryToDecimal",
            "char" or "System.Char" => "TryToChar",
            "System.DateTime" => "TryToDateTime",
            "System.DateTimeOffset" => "TryToDateTimeOffset",
            "System.DateOnly" => "TryToDateOnly",
            "System.TimeOnly" => "TryToTimeOnly",
            "System.TimeSpan" => "TryToTimeSpan",
            "System.Guid" => "TryToGuid",
            "string" or "System.String" => string.Empty,
            _ when type.TypeKind == TypeKind.Enum => "TryToEnum",
            _ => string.Empty
        };
    }

    private static TypeRefModel MakeTypeRef(ITypeSymbol type)
    {
        // Nullable<T> / 配列を再帰的に表現できる TypeRefModel へ正規化する
        // Normalize the Roslyn type into a recursive TypeRefModel that can represent Nullable<T> and arrays
        var isNullable = false;
        TypeRefModel? underlyingType = null;

        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
        {
            isNullable = true;
            underlyingType = MakeTypeRef(namedType.TypeArguments[0]);
        }

        if (type is IArrayTypeSymbol arr)
        {
            return new TypeRefModel(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                false,
                null,
                true,
                MakeTypeRef(arr.ElementType));
        }

        return new TypeRefModel(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isNullable,
            underlyingType,
            false,
            null);
    }
}
