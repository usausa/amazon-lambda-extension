namespace AmazonLambdaExtension.SourceGenerator.Models;

public class TypeModel
{
    public string FullName { get; set; }

    public bool AllowNull { get; set; }

    public bool IsArrayType { get; set; }

    public TypeModel? ElementType { get; set; }

    public TypeModel(string fullName, bool allowNull, bool isArrayType, TypeModel? elementType)
    {
        FullName = fullName;
        AllowNull = allowNull;
        IsArrayType = isArrayType;
        ElementType = elementType;
    }
}

public static class TypeModelExtensions
{
    public static bool IsAPIGatewayProxyRequest(this TypeModel type) => type.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest";

    public static bool IsLambdaContext(this TypeModel type) => type.FullName == "Amazon.Lambda.Core.ILambdaContext";
}
