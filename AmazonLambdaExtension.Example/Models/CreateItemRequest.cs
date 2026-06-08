namespace AmazonLambdaExtension.Example.Models;

using System.ComponentModel.DataAnnotations;

public sealed class CreateItemRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
