namespace AmazonLambdaExtension.Binders;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

public static class CookieReader
{
    private static readonly SearchValues<char> TokenChars = SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~");

    private static readonly SearchValues<char> ValueChars = SearchValues.Create("!#$%&'()*+-./0123456789:<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~");

    // HTTP API (payload v2) passes cookies as "name=value" entries in the cookies array, not in the headers
    public static bool TryGetValue(IEnumerable<string>? cookies, string name, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (cookies is not null)
        {
            foreach (var cookie in cookies)
            {
                var index = SkipSeparators(cookie, 0);
                while (index < cookie.Length)
                {
                    if (TryReadCookie(cookie, ref index, out var cookieName, out var cookieValue) && cookieName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = Uri.UnescapeDataString(cookieValue);
                    }
                }
            }
        }

        return value is not null;
    }

    private static bool TryReadCookie(string input, ref int index, out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
    {
        var start = index;
        index = Skip(input, index, TokenChars);
        name = input.AsSpan(start, index - start);
        value = default;
        if (!name.IsEmpty && (index < input.Length) && (input[index] == '='))
        {
            index++;
            value = ReadValue(input, ref index);
            var end = SkipWhitespace(input, index);
            if ((end == input.Length) || (input[end] == ';'))
            {
                index = SkipSeparators(input, end);
                return !value.IsEmpty;
            }
        }

        var separator = input.IndexOf(';', index);
        index = separator < 0 ? input.Length : SkipSeparators(input, separator);
        return false;
    }

    private static ReadOnlySpan<char> ReadValue(string input, ref int index)
    {
        var start = index;
        var quoted = (index < input.Length) && (input[index] == '"');
        index = Skip(input, quoted ? index + 1 : index, ValueChars);
        if (quoted)
        {
            if ((index == input.Length) || (input[index] != '"'))
            {
                return default;
            }

            index++;
        }

        return input.AsSpan(start, index - start);
    }

    private static int Skip(string input, int index, SearchValues<char> chars)
    {
        var length = input.AsSpan(index).IndexOfAnyExcept(chars);
        return length < 0 ? input.Length : index + length;
    }

    private static int SkipSeparators(string input, int index)
    {
        index = SkipWhitespace(input, index);
        while ((index < input.Length) && (input[index] == ';'))
        {
            index = SkipWhitespace(input, index + 1);
        }

        return index;
    }

    private static int SkipWhitespace(string input, int index)
    {
        while (index < input.Length)
        {
            if (input[index] is ' ' or '\t')
            {
                index++;
            }
            else if ((input[index] == '\r') && ((index + 2) < input.Length) && (input[index + 1] == '\n') && (input[index + 2] is ' ' or '\t'))
            {
                index += 3;
            }
            else
            {
                break;
            }
        }

        return index;
    }
}
