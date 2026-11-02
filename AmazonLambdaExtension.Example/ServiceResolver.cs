namespace AmazonLambdaExtension.Example;

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

        services.AddSingleton<IBodySerializer>(new JsonBodySerializer(AppJsonContext.Default));
        services.AddValidation();
        services.AddSingleton<IRequestValidator, ValidatableTypeRequestValidator>();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSingleton<DataService>();
        services.AddSingleton<IProcessor, Processor>();

        services.AddSingleton<LoggingFilter>();
        services.AddSingleton<ApiKeyFilter>();

        return services;
    }
}
