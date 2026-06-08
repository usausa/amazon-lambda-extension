namespace AmazonLambdaExtension.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor NotPartialClass { get; } = new(
        id: "ALE0001",
        title: "[Lambda] class must be partial",
        messageFormat: "[Lambda] class '{0}' must be declared partial",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor GenericLambdaClass { get; } = new(
        id: "ALE0002",
        title: "[Lambda] class must not be generic",
        messageFormat: "[Lambda] class '{0}' must not be generic",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NestedLambdaClass { get; } = new(
        id: "ALE0003",
        title: "[Lambda] class must be a top-level type",
        messageFormat: "[Lambda] class '{0}' must be a top-level type",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RecordLambdaClass { get; } = new(
        id: "ALE0004",
        title: "[Lambda] record is not supported",
        messageFormat: "[Lambda] record '{0}' is not supported",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AbstractLambdaClass { get; } = new(
        id: "ALE0005",
        title: "[Lambda] class must not be abstract",
        messageFormat: "[Lambda] class '{0}' must not be abstract",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidServiceResolverType { get; } = new(
        id: "ALE0006",
        title: "ServiceResolver type does not have an accessible ConfigureServices method",
        messageFormat: "ServiceResolver type '{0}' does not have an accessible static IServiceCollection ConfigureServices() method",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingServiceResolver { get; } = new(
        id: "ALE0007",
        title: "[Lambda] class has constructor parameters but no [ServiceResolver]",
        messageFormat: "[Lambda] class '{0}' has constructor parameters but no [ServiceResolver]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor LambdaClassNoParameterlessCtor { get; } = new(
        id: "ALE0008",
        title: "[Lambda] class requires a parameterless constructor when [ServiceResolver] is not specified",
        messageFormat: "[Lambda] class '{0}' must declare a parameterless constructor when [ServiceResolver] is not specified",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FilterNotImplementILambdaFilter { get; } = new(
        id: "ALE0009",
        title: "Filter type does not implement ILambdaFilter",
        messageFormat: "Filter type '{0}' does not implement ILambdaFilter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AbstractFilter { get; } = new(
        id: "ALE0010",
        title: "Filter must not be abstract when [ServiceResolver] is not specified",
        messageFormat: "Filter '{0}' must not be abstract when [ServiceResolver] is not specified",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FilterNoParameterlessCtor { get; } = new(
        id: "ALE0011",
        title: "Filter requires an accessible parameterless constructor when [ServiceResolver] is not specified",
        messageFormat: "Filter '{0}' must declare an accessible parameterless constructor when [ServiceResolver] is not specified",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NoHandlerAttribute { get; } = new(
        id: "ALE0012",
        title: "Handler has no recognized handler attribute",
        messageFormat: "Handler '{0}' has no recognized handler attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MultipleHandlerAttributes { get; } = new(
        id: "ALE0013",
        title: "Handler has multiple handler attributes",
        messageFormat: "Handler '{0}' has multiple handler attributes",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerMethodNotFound { get; } = new(
        id: "ALE0014",
        title: "Authorizer method not found",
        messageFormat: "Authorizer method '{0}' was not found in the same class",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MultipleBindingAttributes { get; } = new(
        id: "ALE0015",
        title: "Parameter has multiple binding attributes",
        messageFormat: "Parameter '{0}.{1}' has multiple binding attributes",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FromBodyOnEventHandler { get; } = new(
        id: "ALE0016",
        title: "[FromBody] cannot be used with [Event] handler",
        messageFormat: "[FromBody] cannot be used with [Event] handler '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidEventBinding { get; } = new(
        id: "ALE0017",
        title: "[Event] handler uses unsupported binding attribute",
        messageFormat: "[Event] handler '{0}' cannot use {1} on parameter '{2}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FromCustomAuthorizerOutsideHttpApi { get; } = new(
        id: "ALE0018",
        title: "[FromCustomAuthorizer] is used outside an HTTP API handler",
        messageFormat: "[FromCustomAuthorizer] is used outside an HTTP API handler on '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedBindingType { get; } = new(
        id: "ALE0019",
        title: "Unsupported parameter type for binding",
        messageFormat: "Parameter type '{0}' for [FromQuery] / [FromRoute] / [FromHeader] is unsupported",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EventHandlerMissingPayload { get; } = new(
        id: "ALE0020",
        title: "[Event] handler must declare exactly one event payload parameter",
        messageFormat: "[Event] handler '{0}' must declare exactly one event payload parameter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EventHandlerMultiplePayloads { get; } = new(
        id: "ALE0021",
        title: "[Event] handler has multiple event payload parameters",
        messageFormat: "[Event] handler '{0}' has multiple event payload parameters",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerInvalidReturnType { get; } = new(
        id: "ALE0022",
        title: "[HttpApiAuthorizer] requires IAuthorizerResult return type",
        messageFormat: "[HttpApiAuthorizer] requires IAuthorizerResult return type on '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingServiceResolverForFromServices { get; } = new(
        id: "ALE0023",
        title: "[FromServices] requires [ServiceResolver]",
        messageFormat: "[Lambda] class '{0}' uses [FromServices] but has no [ServiceResolver]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor OverloadedHandler { get; } = new(
        id: "ALE0024",
        title: "Handler is overloaded",
        messageFormat: "Handler '{0}' is overloaded; Lambda handler names must be unique",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
