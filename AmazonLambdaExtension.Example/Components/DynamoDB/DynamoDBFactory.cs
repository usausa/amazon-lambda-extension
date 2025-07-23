namespace AmazonLambdaExtension.Example.Components.DynamoDB;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

public sealed class DynamoDBFactory : IDynamoDBFactory
{
    private readonly IAmazonDynamoDB dynamoClient;

    public DynamoDBFactory(IAmazonDynamoDB dynamoClient)
    {
        this.dynamoClient = dynamoClient;
    }

    public IDynamoDBContext Create() =>
        new DynamoDBContextBuilder()
            .WithDynamoDBClient(() => dynamoClient)
            .Build();
}
