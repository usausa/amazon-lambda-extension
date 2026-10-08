namespace AmazonLambdaExtension.Binders;

using System.ComponentModel.DataAnnotations;

public sealed class BindingError
{
    public BindingErrorKind Kind { get; }

    public BindingSource Source { get; }

    // The binding key (route / query / header / authorizer name) or the body parameter name
    public string Name { get; }

    public IReadOnlyList<ValidationResult> ValidationResults { get; }

    public BindingError(BindingErrorKind kind, BindingSource source, string name, IReadOnlyList<ValidationResult>? validationResults = null)
    {
        Kind = kind;
        Source = source;
        Name = name;
        ValidationResults = validationResults ?? [];
    }
}
