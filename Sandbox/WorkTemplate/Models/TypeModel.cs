namespace WorkTemplate.Models;

using System.Diagnostics.CodeAnalysis;

public class TypeModel
{
    [AllowNull]
    public string FullName { get; set; }

    public bool IsNullable { get; set; } = true;

    public bool IsMultiType { get; set; }
}

public static class TypeModelExtensions
{
    public static bool IsAPIGatewayProxyRequest(this TypeModel type) => type.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest";

    public static bool IsLambdaContext(this TypeModel type) => type.FullName == "Amazon.Lambda.Core.ILambdaContext";
}
