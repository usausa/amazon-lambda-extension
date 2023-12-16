namespace AmazonLambdaExtension.Example.Parameters;

using System.ComponentModel.DataAnnotations;

public sealed class CrudCreateInput
{
    [Required]
    public string Name { get; set; } = default!;
}
