namespace AmazonLambdaExtension.Example;

using System.Text.Json.Serialization;

using Amazon.Lambda.APIGatewayEvents;

using AmazonLambdaExtension.Example.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateItemRequest))]
[JsonSerializable(typeof(ItemResponse))]
[JsonSerializable(typeof(ItemResponse[]))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
