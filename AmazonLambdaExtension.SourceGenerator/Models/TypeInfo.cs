#nullable disable
namespace AmazonLambdaExtension.SourceGenerator.Models;

public class TypeInfo
{
    public string FullName { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsMultiType { get; set; }
}

public static class TypeModelExtensions
{
    public static bool IsAPIGatewayProxyRequest(this TypeInfo type) => type.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest";

    public static bool IsLambdaContext(this TypeInfo type) => type.FullName == "Amazon.Lambda.Core.ILambdaContext";
}
