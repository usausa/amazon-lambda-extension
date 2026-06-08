namespace AmazonLambdaExtension.Example.Models;

public sealed class ItemResponse
{
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string TenantId { get; set; } = default!;
}
