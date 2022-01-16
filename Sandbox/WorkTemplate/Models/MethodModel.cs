namespace WorkTemplate.Models;

using System.Diagnostics.CodeAnalysis;

public class MethodModel
{
    // $"{lambdaMethodSymbol.ContainingType.Name}_{lambdaMethodSymbol.Name}_Generated";
    [AllowNull]
    public string WrapperClassName { get; set; }

    public bool IsAsync { get; set; }

    [AllowNull]
    public string Name { get; set; }

    [AllowNull]
    public ParameterModel[] Parameters { get; set; }

    public TypeModel? ResultType { get; set; }

    public bool HasValidationParameter => Parameters.Any(x => x.ParameterType != ParameterType.FromService);

    public bool HasBodyParameter => Parameters.Any(x => x.ParameterType == ParameterType.FromBody);
}
