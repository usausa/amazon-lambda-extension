namespace AmazonLambdaExtension.SourceGenerator.Models;

public class TypeInfo
{
    public string FullName { get; set; }

    public bool AllowNull { get; set; }

    public bool IsArrayType { get; set; }

    public TypeInfo? ElementType { get; set; }

    public TypeInfo(string fullName, bool allowNull, bool isArrayType, TypeInfo? elementType)
    {
        FullName = fullName;
        AllowNull = allowNull;
        IsArrayType = isArrayType;
        ElementType = elementType;
    }
}

public static class TypeModelExtensions
{
    public static bool IsAPIGatewayProxyRequest(this TypeInfo type) => type.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest";

    public static bool IsLambdaContext(this TypeInfo type) => type.FullName == "Amazon.Lambda.Core.ILambdaContext";
}
