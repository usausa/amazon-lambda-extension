# Diagnostics

## Class structure

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0001 | ❌ Error | `[Lambda]` class is not `partial` | Declare the class as `partial` |
| ALE0002 | ❌ Error | `[Lambda]` class is generic | Remove the type parameters from the class |
| ALE0003 | ❌ Error | `[Lambda]` class is a nested or file-local type | Move the class to the top level, and do not declare it `file` |
| ALE0004 | ❌ Error | `[Lambda]` is applied to a record | Declare the target as a class |
| ALE0005 | ❌ Error | `[Lambda]` class is `abstract` | Make the class non-abstract |

## Construction

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0101 | ❌ Error | `[Lambda]` class has constructor parameters, but no parameterless constructor and no `[ServiceResolver]` | Specify `[ServiceResolver]`, or add a parameterless constructor |
| ALE0102 | ❌ Error | `[ServiceResolver]` type has no accessible `static IServiceCollection ConfigureServices()` method | Add the `ConfigureServices()` method to the resolver type |
| ALE0103 | ❌ Error | `[Lambda]` class has no parameterless constructor (with `[SetsRequiredMembers]` when it has required members) and no `[ServiceResolver]` | Add a parameterless constructor, or specify `[ServiceResolver]` |
| ALE0104 | ❌ Error | `[Lambda]` class has required members, and the constructor the generated code calls has no `[SetsRequiredMembers]` | Mark the constructor with `[SetsRequiredMembers]` and set the members in it |
| ALE0105 | ❌ Error | `[FromServices]` is used but the class has no `[ServiceResolver]` | Specify `[ServiceResolver]` on the `[Lambda]` class |

## Filter

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0201 | ❌ Error | Filter type does not implement `ILambdaFilter` | Implement `ILambdaFilter` on the filter type |
| ALE0202 | ❌ Error | Filter is `abstract` and no `[ServiceResolver]` is specified | Make the filter non-abstract, or specify `[ServiceResolver]` |
| ALE0203 | ❌ Error | Filter has no accessible parameterless constructor (with `[SetsRequiredMembers]` when it has required members) and no `[ServiceResolver]` | Add an accessible parameterless constructor, or specify `[ServiceResolver]` |

## Handler

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0301 | ❌ Error | Handler has multiple handler attributes | Leave a single handler attribute |
| ALE0302 | ❌ Error | Handler is a generic method | Remove the type parameters from the handler |
| ALE0303 | ❌ Error | `[Event]` handler declares no event payload parameter | Declare exactly one event payload parameter |
| ALE0304 | ❌ Error | `[Event]` handler declares multiple event payload parameters | Leave a single event payload parameter |
| ALE0305 | ❌ Error | `[HttpApiAuthorizer]` handler does not return `IAuthorizerResult` | Change the return type to `IAuthorizerResult` |
| ALE0306 | ❌ Error | Handler name is not unique because the handler is overloaded | Rename the handler so that each handler name is unique |
| ALE0307 | ⚠️ Warning | Public method has no recognized handler attribute (interface implementations and overrides are excluded) | Add a handler attribute, or remove the method from the `[Lambda]` class |
| ALE0308 | ⚠️ Warning | Method named by `Authorizer = nameof(...)` is not found in the same class | Correct the authorizer name, or add the method to the class |
| ALE0309 | ⚠️ Warning | Handler is declared in a base class, so it is not generated (reported once at the base method) | Declare the handler in the `[Lambda]` class |

## Parameter

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0401 | ❌ Error | Parameter has multiple binding attributes | Leave a single binding attribute on the parameter |
| ALE0402 | ❌ Error | `[FromBody]` is applied to an `[Event]` handler | Remove `[FromBody]`, or use an HTTP handler |
| ALE0403 | ❌ Error | Binding attribute is not supported on an `[Event]` handler | Remove the binding attribute, or use an HTTP handler |
| ALE0404 | ❌ Error | `ClaimsPrincipal` parameter is used outside an `[HttpApi]` / `[FunctionUrl]` handler | Remove the `ClaimsPrincipal` parameter, or use an `[HttpApi]` / `[FunctionUrl]` handler |
| ALE0405 | ❌ Error | `[FromCookie]` is used outside an `[HttpApi]` / `[FunctionUrl]` handler | Remove `[FromCookie]`, or use an `[HttpApi]` / `[FunctionUrl]` handler |
| ALE0406 | ❌ Error | Parameter type is not supported by binding (`[FromAuthorizer]` / `[FromCookie]` do not bind arrays, and an array bound with `[FromQuery]` / `[FromHeader]` / `[FromRoute]` must be one-dimensional; jagged and multidimensional arrays are not supported) | Use a supported parameter type |
| ALE0407 | ⚠️ Warning | `[FromAuthorizer]` is used outside an HTTP API handler | Remove `[FromAuthorizer]`, or use an HTTP API handler |

> Up to 2.0.0-beta9, ALE0404 reported `[FromClaim]` outside an `[HttpApi]` handler. `[FromClaim]` was removed in favor of the `ClaimsPrincipal` parameter, and the ID is reused for it.

## Generation

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0501 | ❌ Error | Class or handler name differs only in case from another one, so the generated file names collide; only the first (in ordinal order) is generated | Rename one of the classes or handlers |

## Entry point

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0601 | ❌ Error | `[assembly: LambdaEntryPoint]` is used, but `Amazon.Lambda.RuntimeSupport` is not referenced | Add a package reference to `Amazon.Lambda.RuntimeSupport` |
| ALE0602 | ❌ Error | `[assembly: LambdaEntryPoint]` is used, but the assembly has no `[LambdaSerializer]` | Add `[assembly: LambdaSerializer(typeof(...))]` (for Native AOT, `SourceGeneratorLambdaJsonSerializer<TContext>`) |
| ALE0603 | ❌ Error | `[LambdaSerializer]` type is abstract or an open generic type, does not implement `ILambdaSerializer`, or has no accessible parameterless constructor | Specify a concrete `ILambdaSerializer` with an accessible parameterless constructor |
