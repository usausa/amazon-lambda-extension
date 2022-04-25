namespace AmazonLambdaExtension.Example;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using Amazon.DynamoDBv2;

using AmazonLambdaExtension.Example.Components.DynamoDB;
using AmazonLambdaExtension.Example.Components.Json;
using AmazonLambdaExtension.Example.Components.Logging;
using AmazonLambdaExtension.Example.Services;
using AmazonLambdaExtension.Serialization;

using AutoMapper;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class ServiceResolver
{
    private readonly IServiceProvider provider = BuildProvider();

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        // Log
        services.AddLogging(c =>
        {
            c.ClearProviders();
            c.AddLambdaLogger();
        });

        // Serializer
        services.AddSingleton<IBodySerializer>(_ => new JsonBodySerializer(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new DateTimeConverter() }
        }));

        // Dynamo
        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddSingleton<IDynamoDBFactory, DynamoDBFactory>();

        // Http client
        services.AddHttpClient(ConnectorNames.Ipify, c =>
        {
            c.BaseAddress = new Uri("https://api.ipify.org/");
        });

        // Mapper
        services.AddSingleton<IMapper>(_ => new Mapper(new MapperConfiguration(c =>
        {
            c.AddProfile<MappingProfile>();
        })));

        // Service
        services.AddSingleton<DataService>();

        return services.BuildServiceProvider();
    }

    public T? GetService<T>()
    {
        return provider.GetService<T>();
    }
}
