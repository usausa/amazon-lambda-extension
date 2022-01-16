namespace AmazonLambdaExtension.SourceGenerator.Models;

public enum ParameterType
{
    None,
    FromQuery,
    FromBody,
    FromRoute,
    FromHeader,
    FromService
}
