namespace AmazonLambdaExtension.Generator.Models;

internal enum ParameterBindingType
{
    Request,
    Context,
    FromQuery,
    FromHeader,
    FromRoute,
    FromBody,
    FromServices,
    FromCustomAuthorizer
}

internal sealed record ParameterModel(
    string Name,
    TypeRefModel Type,
    ParameterBindingType BindingType,
    string Key,
    string ConverterMethod,
    bool SkipValidation);
