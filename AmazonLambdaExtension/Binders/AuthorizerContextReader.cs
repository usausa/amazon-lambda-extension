namespace AmazonLambdaExtension.Binders;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

public static class AuthorizerContextReader
{
    public static bool TryGetString(IDictionary<string, object?>? context, string key, [NotNullWhen(true)] out string? value)
    {
        value = (context is not null) && context.TryGetValue(key, out var raw) ? Format(raw) : null;
        return value is not null;
    }

    // The values of a real event are deserialized as JsonElement
    private static string? Format(object? raw) => raw switch
    {
        null => null,
        string s => s,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        JsonElement element => element.GetRawText(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => raw.ToString()
    };
}
