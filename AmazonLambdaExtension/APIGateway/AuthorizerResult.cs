namespace AmazonLambdaExtension.APIGateway;

using System.IO;
using System.Text.Json;

internal sealed class AuthorizerResult : IAuthorizerResult
{
    public bool IsAuthorized { get; private set; }

    public string? PrincipalId { get; private set; }

    public IDictionary<string, object>? Context { get; private set; }

    internal AuthorizerResult(bool isAuthorized)
    {
        IsAuthorized = isAuthorized;
    }

    public IAuthorizerResult WithContext(string key, object value)
    {
        Context ??= new Dictionary<string, object>(StringComparer.Ordinal);
        Context[key] = value;
        return this;
    }

    public IAuthorizerResult WithPrincipalId(string principalId)
    {
        PrincipalId = principalId;
        return this;
    }

    public Stream Serialize(AuthorizerResultSerializationOptions options)
    {
        MemoryStream ms = new();
        using (var writer = new Utf8JsonWriter(ms))
        {
            if (options.Format == AuthorizerResultSerializationOptions.AuthorizerFormat.HttpApiSimple)
            {
                WriteSimpleResponse(writer);
            }
            else
            {
                WriteIamPolicyResponse(writer, options.RouteArn);
            }
        }
        ms.Position = 0;
        return ms;
    }

    private void WriteSimpleResponse(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("isAuthorized"u8, IsAuthorized);
        if (Context is not null)
        {
            writer.WriteStartObject("context"u8);
            foreach (var kv in Context)
            {
                WriteContextValue(writer, kv.Key, kv.Value);
            }
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNull("context"u8);
        }
        writer.WriteEndObject();
    }

    private void WriteIamPolicyResponse(Utf8JsonWriter writer, string? routeArn)
    {
        var effect = IsAuthorized ? "Allow" : "Deny";
        var resource = routeArn ?? "*";
        writer.WriteStartObject();
        writer.WriteString("principalId"u8, PrincipalId ?? "user");
        writer.WriteStartObject("policyDocument"u8);
        writer.WriteString("Version"u8, "2012-10-17");
        writer.WriteStartArray("Statement"u8);
        writer.WriteStartObject();
        writer.WriteString("Action"u8, "execute-api:Invoke");
        writer.WriteString("Effect"u8, effect);
        writer.WriteString("Resource"u8, resource);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
        if (Context is not null)
        {
            writer.WriteStartObject("context"u8);
            foreach (var kv in Context)
            {
                WriteContextValue(writer, kv.Key, kv.Value);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static void WriteContextValue(Utf8JsonWriter writer, string key, object value)
    {
        switch (value)
        {
            case string s:
                writer.WriteString(key, s);
                break;
            case bool b:
                writer.WriteBoolean(key, b);
                break;
            case int i:
                writer.WriteNumber(key, i);
                break;
            case long l:
                writer.WriteNumber(key, l);
                break;
            case double d:
                writer.WriteNumber(key, d);
                break;
            default:
                writer.WriteString(key, value.ToString());
                break;
        }
    }
}
