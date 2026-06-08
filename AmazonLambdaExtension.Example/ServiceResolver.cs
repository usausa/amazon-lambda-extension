namespace AmazonLambdaExtension.Example;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

using AmazonLambdaExtension.Example.Filters;
using AmazonLambdaExtension.Example.Services;
using AmazonLambdaExtension.Serialization;
using AmazonLambdaExtension.Validation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public static class ServiceResolver
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILambdaSerializer>(
            new SourceGeneratorLambdaJsonSerializer<AppJsonContext>());

        services.AddSingleton<IBodySerializer>(new JsonBodySerializer(AppJsonContext.Default));

        services.AddSingleton<IRequestValidator, DataAnnotationsRequestValidator>();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSingleton<DataService>();
        services.AddSingleton<IProcessor, Processor>();

        services.AddSingleton<LoggingFilter>();
        services.AddSingleton<ApiKeyFilter>();

        return services;
    }
}
