namespace WorkTemplate.Serializer;

using System.Text.Json;

public interface IBodySerializer
{
    T Deserialize<T>(string body);

    string Serialize<T>(T value);
}

public class JsonBodySerializer : IBodySerializer
{
    public static JsonBodySerializer Default => new(new JsonSerializerOptions());

    private readonly JsonSerializerOptions options;

    public JsonBodySerializer(JsonSerializerOptions options)
    {
        this.options = options;
    }

    public T Deserialize<T>(string body) => JsonSerializer.Deserialize<T>(body, options)!;

    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, options);
}
