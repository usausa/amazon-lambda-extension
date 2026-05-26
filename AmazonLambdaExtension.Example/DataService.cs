namespace AmazonLambdaExtension.Example;

using System.Globalization;
// ReSharper disable MemberCanBeMadeStatic.Global
#pragma warning disable CA1822
public sealed class DataService
{
    public ValueTask<Item?> GetAsync(string id, int page)
    {
        if (id == "not-exist")
        {
            return ValueTask.FromResult<Item?>(null);
        }
        var item = new Item { Id = id, Name = $"Item-{id}-Page{page.ToString(CultureInfo.InvariantCulture)}", TenantId = "tenant-1" };
        return ValueTask.FromResult<Item?>(item);
    }

    public ValueTask<Item[]> ListAsync(string tenantId, int[] ids)
    {
        var items = ids.Select(id => new Item
        {
            Id = id.ToString(CultureInfo.InvariantCulture),
            Name = $"Item-{id}",
            TenantId = tenantId
        }).ToArray();
        return ValueTask.FromResult(items);
    }

    public ValueTask<Item> CreateAsync(CreateItemInput input)
    {
        var item = new Item
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = input.Name,
            TenantId = "tenant-1"
        };
        return ValueTask.FromResult(item);
    }

    public ValueTask<bool> IsValidTokenAsync(string token)
    {
        return ValueTask.FromResult(token == "valid-token");
    }
}
#pragma warning restore CA1822
// ReSharper restore MemberCanBeMadeStatic.Global
