namespace AmazonLambdaExtension.Example;

using System.ComponentModel.DataAnnotations;

public sealed class Item
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}

public sealed class CreateItemInput
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
