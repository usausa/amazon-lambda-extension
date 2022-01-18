namespace AmazonLambdaExtension.SourceGenerator.Models;

public class ParameterInfo
{
    public string Name { get; set; }

    public TypeInfo Type { get; set; }

    public ParameterType ParameterType { get; set; }

    public string Key { get; set; }

    public ParameterInfo(string name, TypeInfo type, ParameterType parameterType, string? key = null)
    {
        Name = name;
        Type = type;
        ParameterType = parameterType;
        Key = key ?? name;
    }
}
