namespace AmazonLambdaExtension.Example;

using System.Text.Json.Serialization;

using AmazonLambdaExtension.Example.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateItemRequest))]
[JsonSerializable(typeof(ItemResponse))]
[JsonSerializable(typeof(ItemResponse[]))]
[JsonSerializable(typeof(MeResponse))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
