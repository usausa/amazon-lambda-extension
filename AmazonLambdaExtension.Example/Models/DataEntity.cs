namespace AmazonLambdaExtension.Example.Models;

using System.Diagnostics.CodeAnalysis;

using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("ExampleData")]
public class DataEntity
{
    [DynamoDBHashKey]
    [AllowNull]
    public string Id { get; set; }

    [AllowNull]
    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }
}
