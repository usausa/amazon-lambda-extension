namespace AmazonLambdaExtension.Example;

using System.Text.Json.Serialization;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

using AmazonLambdaExtension.Serialization;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceResolver
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILambdaSerializer>(
            new SourceGeneratorLambdaJsonSerializer<AppJsonContext>());

        services.AddSingleton<IBodySerializer>(new JsonBodySerializer(AppJsonContext.Default));

        services.AddSingleton<DataService>();
        services.AddSingleton<IProcessor, MockProcessor>();

        services.AddSingleton<LoggingFilter>();
        services.AddSingleton<ApiKeyFilter>();

        return services;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CreateItemInput))]
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(Item[]))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
internal partial class AppJsonContext : JsonSerializerContext;
