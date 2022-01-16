#nullable disable
namespace AmazonLambdaExtension.SourceGenerator.Models;

public class ParameterInfo
{
    public string Name { get; set; }

    public TypeInfo Type { get; set; }

    public ParameterType ParameterType { get; set; }
}
