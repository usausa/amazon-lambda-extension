namespace AmazonLambdaExtension.Serialization;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

public sealed class JsonBodySerializer : IBodySerializer
{
    public static JsonBodySerializer Default
    {
        [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed. Use the JsonSerializerContext overload.")]
        [RequiresDynamicCode("JSON serialization may require dynamic code generation. Use the JsonSerializerContext overload.")]
        get
        {
            var instance = Volatile.Read(ref field);
            if (instance is not null)
            {
                return instance;
            }

            var created = new JsonBodySerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            return Interlocked.CompareExchange(ref field, created, null) ?? created;
        }
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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection path is used only when constructed with JsonSerializerOptions. The caller already opted in to reflection via that constructor.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection path is used only when constructed with JsonSerializerOptions. The caller already opted in to reflection via that constructor.")]
    public T Deserialize<T>(string body)
    {
        if (context is not null)
        {
            return (T)JsonSerializer.Deserialize(body, typeof(T), context)!;
        }
        return JsonSerializer.Deserialize<T>(body, options)!;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection path is used only when constructed with JsonSerializerOptions. The caller already opted in to reflection via that constructor.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection path is used only when constructed with JsonSerializerOptions. The caller already opted in to reflection via that constructor.")]
    public string Serialize<T>(T value)
    {
        if (context is not null)
        {
            return JsonSerializer.Serialize(value, typeof(T), context);
        }
        return JsonSerializer.Serialize(value, options);
    }
}
