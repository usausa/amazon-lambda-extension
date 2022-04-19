namespace AmazonLambdaExtension.Example.Models;

using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("ExampleData")]
public class DataEntity
{
    [DynamoDBHashKey]
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
