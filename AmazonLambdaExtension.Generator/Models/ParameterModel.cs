namespace AmazonLambdaExtension.Generator.Models;

// TODO Ex:Refactor
public sealed class ParameterModel
{
    public string Name { get; set; }

    public TypeModel Type { get; set; }

    public ParameterType ParameterType { get; set; }

    public bool SkipValidation { get; set; }

    public string Key { get; set; }

    public ParameterModel(string name, TypeModel type, ParameterType parameterType, bool skipValidation, string? key = null)
    {
        Name = name;
        Type = type;
        ParameterType = parameterType;
        SkipValidation = skipValidation;
        Key = key ?? name;
    }
}
