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

    public static DiagnosticDescriptor NoHandlerAttribute { get; } = new(
        id: "ALE0002",
        title: "Handler has no recognized handler attribute",
        messageFormat: "Handler '{0}' has no recognized handler attribute",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MultipleHandlerAttributes { get; } = new(
        id: "ALE0003",
        title: "Handler has multiple handler attributes",
        messageFormat: "Handler '{0}' has multiple handler attributes",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MultipleBindingAttributes { get; } = new(
        id: "ALE0004",
        title: "Parameter has multiple binding attributes",
        messageFormat: "Parameter '{0}.{1}' has multiple binding attributes",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FromBodyOnEventHandler { get; } = new(
        id: "ALE0005",
        title: "[FromBody] cannot be used with [Event] handler",
        messageFormat: "[FromBody] cannot be used with [Event] handler '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerMethodNotFound { get; } = new(
        id: "ALE0006",
        title: "Authorizer method not found",
        messageFormat: "Authorizer method '{0}' was not found in the same class",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AuthorizerInvalidReturnType { get; } = new(
        id: "ALE0007",
        title: "[HttpApiAuthorizer] requires IAuthorizerResult return type",
        messageFormat: "[HttpApiAuthorizer] requires IAuthorizerResult return type on '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FromCustomAuthorizerOutsideHttpApi { get; } = new(
        id: "ALE0008",
        title: "[FromCustomAuthorizer] is used outside an HTTP API handler",
        messageFormat: "[FromCustomAuthorizer] is used outside an HTTP API handler on '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedBindingType { get; } = new(
        id: "ALE0009",
        title: "Unsupported parameter type for binding",
        messageFormat: "Parameter type '{0}' for [FromQuery] / [FromRoute] / [FromHeader] is unsupported",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingServiceResolver { get; } = new(
        id: "ALE0010",
        title: "[Lambda] class has constructor parameters but no [ServiceResolver]",
        messageFormat: "[Lambda] class '{0}' has constructor parameters but no [ServiceResolver]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidServiceResolverType { get; } = new(
        id: "ALE0011",
        title: "ServiceResolver type does not have ConfigureServices method",
        messageFormat: "ServiceResolver type '{0}' does not have a public static IServiceCollection ConfigureServices() method",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FilterNotImplementILambdaFilter { get; } = new(
        id: "ALE0012",
        title: "Filter type does not implement ILambdaFilter",
        messageFormat: "Filter type '{0}' does not implement ILambdaFilter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidEventBinding { get; } = new(
        id: "ALE0013",
        title: "[Event] handler uses unsupported binding attribute",
        messageFormat: "[Event] handler '{0}' cannot use {1} on parameter '{2}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MissingServiceResolverForFromServices { get; } = new(
        id: "ALE0014",
        title: "[FromServices] requires [ServiceResolver]",
        messageFormat: "[Lambda] class '{0}' uses [FromServices] but has no [ServiceResolver]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
