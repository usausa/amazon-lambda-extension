namespace AmazonLambdaExtension.Example.Parameters;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public class CrudCreateInput
{
    [Required]
    [AllowNull]
    public string Name { get; set; }
}
