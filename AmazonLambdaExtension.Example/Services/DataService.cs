namespace AmazonLambdaExtension.Example.Services;

using AmazonLambdaExtension.Example.Components.DynamoDB;
using AmazonLambdaExtension.Example.Models;

public sealed class DataService
{
    private readonly IDynamoDBFactory dynamoDBFactory;

    public DataService(IDynamoDBFactory dynamoDBFactory)
    {
        this.dynamoDBFactory = dynamoDBFactory;
    }

    public async ValueTask<DataEntity?> QueryDataAsync(string id)
    {
        using var context = dynamoDBFactory.Create();
        return await context.LoadAsync<DataEntity>(id).ConfigureAwait(false);
    }

    public async ValueTask CreateDataAsync(DataEntity entity)
    {
        using var context = dynamoDBFactory.Create();
        await context.SaveAsync(entity).ConfigureAwait(false);
    }

    public async ValueTask DeleteDataAsync(string id)
    {
        using var context = dynamoDBFactory.Create();
        await context.DeleteAsync<DataEntity>(id).ConfigureAwait(false);
    }
}
