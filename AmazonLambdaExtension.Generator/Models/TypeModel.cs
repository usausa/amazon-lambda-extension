namespace AmazonLambdaExtension.Generator.Models;

// TODO record
public sealed class TypeModel
{
    public string FullName { get; set; }

    public bool AllowNull { get; set; }

    public bool IsNullable { get; set; }

    public TypeModel? UnderlyingType { get; set; }

    public bool IsArrayType { get; set; }

    public TypeModel? ElementType { get; set; }

    public TypeModel(string fullName, bool allowNull, bool isNullable, TypeModel? underlyingType, bool isArrayType, TypeModel? elementType)
    {
        FullName = fullName;
        AllowNull = allowNull;
        IsNullable = isNullable;
        UnderlyingType = underlyingType;
        IsArrayType = isArrayType;
        ElementType = elementType;
    }
}

public static class TypeModelExtensions
{
    public static bool IsAPIGatewayProxyRequest(this TypeModel type) => type.FullName == "Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest";

    public static bool IsLambdaContext(this TypeModel type) => type.FullName == "Amazon.Lambda.Core.ILambdaContext";
}
