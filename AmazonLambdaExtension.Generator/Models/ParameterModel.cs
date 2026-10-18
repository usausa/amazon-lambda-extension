namespace AmazonLambdaExtension.Generator.Models;

internal enum ParameterBindingType
{
    Request,
    Context,
    CancellationToken,
    User,
    FromQuery,
    FromHeader,
    FromRoute,
    FromBody,
    FromServices,
    FromAuthorizer,
    FromCookie
}

internal sealed record ParameterModel(
    string Name,
    TypeRefModel Type,
    ParameterBindingType BindingType,
    string Key,
    string ConverterMethod,
    bool SkipValidation,
    bool HasDefault,
    string? DefaultValueLiteral);

internal static class ParameterModelExtensions
{
    public static bool IsNullableBodyParameter(this ParameterModel param)
        => param.Type.IsNullable || (param.Type.IsReferenceType && param.Type.IsNullableReferenceType);
}
