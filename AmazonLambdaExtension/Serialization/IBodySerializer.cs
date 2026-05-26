namespace AmazonLambdaExtension.Serialization;

public interface IBodySerializer
{
    T Deserialize<T>(string body);

    string Serialize<T>(T value);
}
