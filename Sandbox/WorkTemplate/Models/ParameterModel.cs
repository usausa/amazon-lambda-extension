namespace WorkTemplate.Models;

using System.Diagnostics.CodeAnalysis;

public class ParameterModel
{
    [AllowNull]
    public string Name { get; set; }

    [AllowNull]
    public TypeModel Type { get; set; }

    public ParameterType ParameterType { get; set; }
}
