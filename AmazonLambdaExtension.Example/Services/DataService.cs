namespace AmazonLambdaExtension.Example.Services;

using System.Globalization;

using AmazonLambdaExtension.Example.Models;

// ReSharper disable MemberCanBeMadeStatic.Global
#pragma warning disable CA1822
public sealed class DataService
{
    public ValueTask<ItemResponse?> GetAsync(string id, int page)
    {
        if (id == "not-exist")
        {
            return ValueTask.FromResult<ItemResponse?>(null);
        }

        return ValueTask.FromResult<ItemResponse?>(new ItemResponse
        {
            Id = id,
            Name = $"Item-{id}-Page{page.ToString(CultureInfo.InvariantCulture)}",
            TenantId = "tenant-1"
        });
    }

    public ValueTask<ItemResponse[]> ListAsync(string tenantId, int[] ids)
    {
        return ValueTask.FromResult(ids.Select(id => new ItemResponse
        {
            Id = id.ToString(CultureInfo.InvariantCulture),
            Name = $"Item-{id}",
            TenantId = tenantId
        }).ToArray());
    }

    public ValueTask<ItemResponse> CreateAsync(CreateItemRequest input)
    {
        return ValueTask.FromResult(new ItemResponse
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = input.Name,
            TenantId = "tenant-1"
        });
    }

    public ValueTask<bool> IsValidTokenAsync(string token)
    {
        return ValueTask.FromResult(token == "valid-token");
    }
}
#pragma warning restore CA1822
// ReSharper restore MemberCanBeMadeStatic.Global
