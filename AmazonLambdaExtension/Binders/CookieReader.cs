namespace AmazonLambdaExtension.Binders;

using System.Diagnostics.CodeAnalysis;

public static class CookieReader
{
    // HTTP API (payload v2) passes cookies as "name=value" entries in the cookies array, not in the headers
    public static bool TryGetValue(IEnumerable<string>? cookies, string name, [NotNullWhen(true)] out string? value)
    {
        if (cookies is not null)
        {
            foreach (var cookie in cookies)
            {
                var index = cookie.IndexOf('=', StringComparison.Ordinal);
                if ((index > 0) && cookie.AsSpan(0, index).Trim().Equals(name, StringComparison.Ordinal))
                {
                    value = cookie[(index + 1)..].Trim();
                    return true;
                }
            }
        }

        value = null;
        return false;
    }
}
