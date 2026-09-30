# Diagnostics

## Class structure

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0001 | ❌ Error | `[Lambda]` class is not `partial` | Declare the class as `partial` |
| ALE0002 | ❌ Error | `[Lambda]` class is generic | Remove the type parameters from the class |
| ALE0003 | ❌ Error | `[Lambda]` class is a nested or file-local type | Move the class to the top level, and do not declare it `file` |
| ALE0004 | ❌ Error | `[Lambda]` is applied to a record | Declare the target as a class |
| ALE0005 | ❌ Error | `[Lambda]` class is `abstract` | Make the class non-abstract |

## DI / generation

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0006 | ❌ Error | `[ServiceResolver]` type has no accessible `static IServiceCollection ConfigureServices()` method | Add the `ConfigureServices()` method to the resolver type |
| ALE0007 | ❌ Error | `[Lambda]` class has constructor parameters, but no parameterless constructor and no `[ServiceResolver]` | Specify `[ServiceResolver]`, or add a parameterless constructor |
| ALE0008 | ❌ Error | `[Lambda]` class has no parameterless constructor (with `[SetsRequiredMembers]` when it has required members) and no `[ServiceResolver]` | Add a parameterless constructor, or specify `[ServiceResolver]` |

## Filter

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0009 | ❌ Error | Filter type does not implement `ILambdaFilter` | Implement `ILambdaFilter` on the filter type |
| ALE0010 | ❌ Error | Filter is `abstract` and no `[ServiceResolver]` is specified | Make the filter non-abstract, or specify `[ServiceResolver]` |
| ALE0011 | ❌ Error | Filter has no accessible parameterless constructor (with `[SetsRequiredMembers]` when it has required members) and no `[ServiceResolver]` | Add an accessible parameterless constructor, or specify `[ServiceResolver]` |

## Handler / parameter

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0012 | ⚠️ Warning | Public method has no recognized handler attribute (interface implementations and overrides are excluded) | Add a handler attribute, or remove the method from the `[Lambda]` class |
| ALE0013 | ❌ Error | Handler has multiple handler attributes | Leave a single handler attribute |
| ALE0014 | ⚠️ Warning | Method named by `Authorizer = nameof(...)` is not found in the same class | Correct the authorizer name, or add the method to the class |
| ALE0015 | ❌ Error | Parameter has multiple binding attributes | Leave a single binding attribute on the parameter |
| ALE0016 | ❌ Error | `[FromBody]` is applied to an `[Event]` handler | Remove `[FromBody]`, or use an HTTP handler |
| ALE0017 | ❌ Error | Binding attribute is not supported on an `[Event]` handler | Remove the binding attribute, or use an HTTP handler |
| ALE0018 | ⚠️ Warning | `[FromAuthorizer]` is used outside an HTTP API handler | Remove `[FromAuthorizer]`, or use an HTTP API handler |
| ALE0019 | ❌ Error | Parameter type is not supported by binding (`[FromAuthorizer]` does not bind arrays) | Use a supported parameter type |
| ALE0020 | ❌ Error | `[Event]` handler declares no event payload parameter | Declare exactly one event payload parameter |
| ALE0021 | ❌ Error | `[Event]` handler declares multiple event payload parameters | Leave a single event payload parameter |
| ALE0022 | ❌ Error | `[HttpApiAuthorizer]` handler does not return `IAuthorizerResult` | Change the return type to `IAuthorizerResult` |
| ALE0025 | ❌ Error | Handler is a generic method | Remove the type parameters from the handler |

## Post-collection

| ID | Severity | Description | How to fix |
|---|---|---|---|
| ALE0023 | ❌ Error | `[FromServices]` is used but the class has no `[ServiceResolver]` | Specify `[ServiceResolver]` on the `[Lambda]` class |
| ALE0024 | ❌ Error | Handler name is not unique because the handler is overloaded | Rename the handler so that each handler name is unique |
| ALE0026 | ❌ Error | `[Lambda]` class has required members, and the constructor the generated code calls has no `[SetsRequiredMembers]` | Mark the constructor with `[SetsRequiredMembers]` and set the members in it |
| ALE0027 | ❌ Error | Class or handler name differs only in case from another one, so the generated file names collide; only the first (in ordinal order) is generated | Rename one of the classes or handlers |
| ALE0028 | ⚠️ Warning | Handler is declared in a base class, so it is not generated (reported once at the base method) | Declare the handler in the `[Lambda]` class |
