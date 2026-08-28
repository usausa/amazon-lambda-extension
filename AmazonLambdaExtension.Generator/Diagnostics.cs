namespace AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    // Class structure (ALE0001-ALE0005)
    public static DiagnosticDescriptor NotPartialClass { get; } = new(
        id: "ALE0001",
        title: "Class must be partial",
        messageFormat: "[Lambda] class must be partial. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor GenericLambdaClass { get; } = new(
        id: "ALE0002",
        title: "Class must not be generic",
        messageFormat: "[Lambda] class must not be generic. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NestedLambdaClass { get; } = new(
        id: "ALE0003",
        title: "Class must not be nested",
        messageFormat: "[Lambda] class must not be nested. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RecordLambdaClass { get; } = new(
        id: "ALE0004",
        title: "Record is not supported",
        messageFormat: "[Lambda] record is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AbstractLambdaClass { get; } = new(
        id: "ALE0005",
        title: "Class must not be abstract",
        messageFormat: "[Lambda] class must not be abstract. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // DI / generation (ALE0006-ALE0008)
    public static DiagnosticDescriptor InvalidServiceResolverType { get; } = new(
        id: "ALE0006",
        title: "Invalid ServiceResolver type",
        messageFormat: "ServiceResolver has no ConfigureServices. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingServiceResolver { get; } = new(
        id: "ALE0007",
        title: "Missing ServiceResolver",
        messageFormat: "Constructor parameters need [ServiceResolver]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor LambdaClassNoParameterlessCtor { get; } = new(
        id: "ALE0008",
        title: "No parameterless constructor",
        messageFormat: "Class has no parameterless constructor. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Filter (ALE0009-ALE0011)
    public static DiagnosticDescriptor FilterNotImplementILambdaFilter { get; } = new(
        id: "ALE0009",
        title: "Invalid filter type",
        messageFormat: "Filter type does not implement ILambdaFilter. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AbstractFilter { get; } = new(
        id: "ALE0010",
        title: "Filter must not be abstract",
        messageFormat: "Filter type is abstract. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FilterNoParameterlessCtor { get; } = new(
        id: "ALE0011",
        title: "Invalid filter constructor",
        messageFormat: "Filter has no parameterless constructor. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Handler / parameter (ALE0012-ALE0022)
    public static DiagnosticDescriptor NoHandlerAttribute { get; } = new(
        id: "ALE0012",
        title: "No handler attribute",
        messageFormat: "Handler has no recognized attribute. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MultipleHandlerAttributes { get; } = new(
        id: "ALE0013",
        title: "Multiple handler attributes",
        messageFormat: "Handler has multiple handler attributes. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerMethodNotFound { get; } = new(
        id: "ALE0014",
        title: "Authorizer method not found",
        messageFormat: "Authorizer method is not in the same class. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MultipleBindingAttributes { get; } = new(
        id: "ALE0015",
        title: "Multiple binding attributes",
        messageFormat: "Parameter has multiple binding attributes. handler=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FromBodyOnEventHandler { get; } = new(
        id: "ALE0016",
        title: "[FromBody] on [Event] handler",
        messageFormat: "[FromBody] cannot be used with [Event]. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidEventBinding { get; } = new(
        id: "ALE0017",
        title: "Invalid Event binding",
        messageFormat: "[Event] handler cannot use this binding. handler=[{0}], attribute=[{1}], parameter=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FromAuthorizerOutsideHttpApi { get; } = new(
        id: "ALE0018",
        title: "[FromAuthorizer] outside HTTP API",
        messageFormat: "[FromAuthorizer] needs an HTTP API handler. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedBindingType { get; } = new(
        id: "ALE0019",
        title: "Unsupported binding type",
        messageFormat: "Binding type is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EventHandlerMissingPayload { get; } = new(
        id: "ALE0020",
        title: "Missing event payload",
        messageFormat: "[Event] handler has no payload parameter. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EventHandlerMultiplePayloads { get; } = new(
        id: "ALE0021",
        title: "Multiple event payloads",
        messageFormat: "Multiple event payload parameters. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerInvalidReturnType { get; } = new(
        id: "ALE0022",
        title: "Invalid authorizer return type",
        messageFormat: "Return type must be IAuthorizerResult. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Post-collection (ALE0023-ALE0024)
    public static DiagnosticDescriptor MissingServiceResolverForFromServices { get; } = new(
        id: "ALE0023",
        title: "Missing ServiceResolver for FromServices",
        messageFormat: "[FromServices] needs [ServiceResolver]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor OverloadedHandler { get; } = new(
        id: "ALE0024",
        title: "Handler is overloaded",
        messageFormat: "Handler name is not unique. handler=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
