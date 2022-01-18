namespace AmazonLambdaExtension.SourceGenerator.Models;

public class ParameterModel
{
    public string Name { get; set; }

    public TypeModel Type { get; set; }

    public ParameterType ParameterType { get; set; }

    public string Key { get; set; }

    public ParameterModel(string name, TypeModel type, ParameterType parameterType, string? key = null)
    {
        Name = name;
        Type = type;
        ParameterType = parameterType;
        Key = key ?? name;
    }
}
