namespace AmazonLambdaExtension.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class JsonBodySerializer : IBodySerializer
{
    public static JsonBodySerializer Default => new(new JsonSerializerOptions(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    }));

    private readonly JsonSerializerOptions options;

    public JsonBodySerializer(JsonSerializerOptions options)
    {
        this.options = options;
    }

    public T Deserialize<T>(string body) => JsonSerializer.Deserialize<T>(body, options)!;

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, options);
}
