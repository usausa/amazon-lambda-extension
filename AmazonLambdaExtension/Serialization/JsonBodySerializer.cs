namespace AmazonLambdaExtension.Serialization;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class JsonBodySerializer : IBodySerializer
{
    public static JsonBodySerializer Default
    {
        [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed. Use the JsonSerializerContext overload.")]
        [RequiresDynamicCode("JSON serialization may require dynamic code generation. Use the JsonSerializerContext overload.")]
        get => new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });
    }

    private readonly JsonSerializerOptions? options;
    private readonly JsonSerializerContext? context;

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed. Use the JsonSerializerContext overload.")]
    [RequiresDynamicCode("JSON serialization may require dynamic code generation. Use the JsonSerializerContext overload.")]
    public JsonBodySerializer(JsonSerializerOptions options)
    {
        this.options = options;
    }

    public JsonBodySerializer(JsonSerializerContext context)
    {
        this.context = context;
    }

    public T Deserialize<T>(string body)
    {
        if (context is not null)
        {
            return (T)JsonSerializer.Deserialize(body, typeof(T), context)!;
        }
        return JsonSerializer.Deserialize<T>(body, options)!;
    }

    public string Serialize<T>(T value)
    {
        if (context is not null)
        {
            return JsonSerializer.Serialize(value, typeof(T), context);
        }
        return JsonSerializer.Serialize(value, options);
    }
}
