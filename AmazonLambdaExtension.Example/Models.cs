namespace AmazonLambdaExtension.Example;

public sealed class Item
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}

public sealed class CreateItemInput
{
    public string Name { get; set; } = string.Empty;
}
