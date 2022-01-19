namespace AmazonLambdaExtension.Example.Services;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using AmazonLambdaExtension.Example.Models;

public class DataService
{
    private readonly IAmazonDynamoDB dynamoClient;

    public DataService(IAmazonDynamoDB dynamoClient)
    {
        this.dynamoClient = dynamoClient;
    }

    public async ValueTask<DataEntity?> QueryDataAsync(string id)
    {
        using var context = new DynamoDBContext(dynamoClient);
        return await context.LoadAsync<DataEntity>(id).ConfigureAwait(false);
    }

    public async ValueTask CreateDataAsync(DataEntity entity)
    {
        using var context = new DynamoDBContext(dynamoClient);
        await context.SaveAsync(entity).ConfigureAwait(false);
    }

    public async ValueTask DeleteDataAsync(string id)
    {
        using var context = new DynamoDBContext(dynamoClient);
        await context.DeleteAsync<DataEntity>(id).ConfigureAwait(false);
    }
}
