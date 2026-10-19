namespace AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal static class Diagnostics
{
    // Class structure (00xx)

    public static DiagnosticDescriptor NotPartialClass { get; } = new(
        id: "ALE0001",
        title: "Class must be partial",
        messageFormat: "[Lambda] class must be partial. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor GenericLambdaClass { get; } = new(
        id: "ALE0002",
        title: "Class must not be generic",
        messageFormat: "[Lambda] class must not be generic. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NestedLambdaClass { get; } = new(
        id: "ALE0003",
        title: "Class must not be nested",
        messageFormat: "[Lambda] class must not be nested or file-local. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor RecordLambdaClass { get; } = new(
        id: "ALE0004",
        title: "Record is not supported",
        messageFormat: "[Lambda] record is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor AbstractLambdaClass { get; } = new(
        id: "ALE0005",
        title: "Class must not be abstract",
        messageFormat: "[Lambda] class must not be abstract. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Construction (01xx)

    public static DiagnosticDescriptor MissingServiceResolver { get; } = new(
        id: "ALE0101",
        title: "Missing ServiceResolver",
        messageFormat: "Constructor parameters need [ServiceResolver]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidServiceResolverType { get; } = new(
        id: "ALE0102",
        title: "Invalid ServiceResolver type",
        messageFormat: "[ServiceResolver] type has no ConfigureServices. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor LambdaClassNoParameterlessCtor { get; } = new(
        id: "ALE0103",
        title: "No parameterless constructor",
        messageFormat: "Class has no parameterless constructor (with [SetsRequiredMembers] when it has required members). type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor RequiredMembersNotSet { get; } = new(
        id: "ALE0104",
        title: "Required members are not set",
        messageFormat: "Class has required members, and the constructor the generated code calls has no [SetsRequiredMembers]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor MissingServiceResolverForFromServices { get; } = new(
        id: "ALE0105",
        title: "Missing ServiceResolver for FromServices",
        messageFormat: "[FromServices] needs [ServiceResolver]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Filter (02xx)

    public static DiagnosticDescriptor FilterNotImplementILambdaFilter { get; } = new(
        id: "ALE0201",
        title: "Invalid filter type",
        messageFormat: "Filter type does not implement ILambdaFilter. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor AbstractFilter { get; } = new(
        id: "ALE0202",
        title: "Filter must not be abstract",
        messageFormat: "Filter type is abstract. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor FilterNoParameterlessCtor { get; } = new(
        id: "ALE0203",
        title: "Invalid filter constructor",
        messageFormat: "Filter has no parameterless constructor (with [SetsRequiredMembers] when it has required members). type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Handler (03xx)

    public static DiagnosticDescriptor MultipleHandlerAttributes { get; } = new(
        id: "ALE0301",
        title: "Multiple handler attributes",
        messageFormat: "Handler has multiple handler attributes. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor GenericHandler { get; } = new(
        id: "ALE0302",
        title: "Handler must not be generic",
        messageFormat: "Handler must not be generic. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor EventHandlerMissingPayload { get; } = new(
        id: "ALE0303",
        title: "Missing event payload",
        messageFormat: "[Event] handler has no payload parameter. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor EventHandlerMultiplePayloads { get; } = new(
        id: "ALE0304",
        title: "Multiple event payloads",
        messageFormat: "[Event] handler has multiple payload parameters. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor AuthorizerInvalidReturnType { get; } = new(
        id: "ALE0305",
        title: "Invalid authorizer return type",
        messageFormat: "[HttpApiAuthorizer] return type must be IAuthorizerResult. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor OverloadedHandler { get; } = new(
        id: "ALE0306",
        title: "Handler is overloaded",
        messageFormat: "Handler name is not unique. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NoHandlerAttribute { get; } = new(
        id: "ALE0307",
        title: "No handler attribute",
        messageFormat: "Handler has no recognized attribute. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerMethodNotFound { get; } = new(
        id: "ALE0308",
        title: "Authorizer method not found",
        messageFormat: "Authorizer method is not in the same class. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor BaseClassHandler { get; } = new(
        id: "ALE0309",
        title: "Handler declared in a base class",
        messageFormat: "Handler declared in a base class is not generated. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Parameter (04xx)

    public static DiagnosticDescriptor MultipleBindingAttributes { get; } = new(
        id: "ALE0401",
        title: "Multiple binding attributes",
        messageFormat: "Parameter has multiple binding attributes. handler=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor FromBodyOutsideHttpHandler { get; } = new(
        id: "ALE0402",
        title: "[FromBody] outside HTTP handler",
        messageFormat: "[FromBody] needs an HTTP API or Function URL handler. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidEventBinding { get; } = new(
        id: "ALE0403",
        title: "Invalid Event binding",
        messageFormat: "[Event] handler cannot use this binding. handler=[{0}], attribute=[{1}], parameter=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor ClaimsPrincipalOutsideHttpHandler { get; } = new(
        id: "ALE0404",
        title: "ClaimsPrincipal outside HTTP handler",
        messageFormat: "ClaimsPrincipal parameter needs an HTTP API or Function URL handler. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor FromCookieOutsideHttpHandler { get; } = new(
        id: "ALE0405",
        title: "[FromCookie] outside HTTP handler",
        messageFormat: "[FromCookie] needs an HTTP API or Function URL handler. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor UnsupportedBindingType { get; } = new(
        id: "ALE0406",
        title: "Unsupported binding type",
        messageFormat: "Binding type is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor FromAuthorizerOutsideHttpApi { get; } = new(
        id: "ALE0407",
        title: "[FromAuthorizer] outside HTTP API",
        messageFormat: "[FromAuthorizer] needs an HTTP API handler. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingRouteParameter { get; } = new(
        id: "ALE0410",
        title: "Missing route parameter",
        messageFormat: "Route variable is not bound with [FromRoute]. variable=[{0}], handler=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Generation (05xx)

    public static DiagnosticDescriptor HintNameCollision { get; } = new(
        id: "ALE0501",
        title: "Name differs only in case",
        messageFormat: "Class or handler name differs only in case from another one, and its source is not generated. name=[{0}], other=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Entry point (06xx)

    public static DiagnosticDescriptor MissingRuntimeSupport { get; } = new(
        id: "ALE0601",
        title: "Missing Amazon.Lambda.RuntimeSupport",
        messageFormat: "[LambdaEntryPoint] needs a reference to Amazon.Lambda.RuntimeSupport. assembly=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor MissingLambdaSerializer { get; } = new(
        id: "ALE0602",
        title: "Missing LambdaSerializer",
        messageFormat: "[LambdaEntryPoint] needs an assembly-level [LambdaSerializer]. assembly=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidLambdaSerializer { get; } = new(
        id: "ALE0603",
        title: "Invalid LambdaSerializer type",
        messageFormat: "[LambdaSerializer] type must be a concrete ILambdaSerializer with an accessible parameterless constructor. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor TopLevelStatementsWithEntryPoint { get; } = new(
        id: "ALE0604",
        title: "Top-level statements with LambdaEntryPoint",
        messageFormat: "[LambdaEntryPoint] cannot be used with top-level statements, because the generated Main is ignored",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor EntryPointNotExecutable { get; } = new(
        id: "ALE0605",
        title: "LambdaEntryPoint needs an executable",
        messageFormat: "[LambdaEntryPoint] with PublishAot needs OutputType Exe to produce the bootstrap executable. assembly=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);
}
