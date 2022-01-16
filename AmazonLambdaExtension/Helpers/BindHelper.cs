namespace AmazonLambdaExtension.Helpers;

using System.Globalization;

public static class BindHelper
{
//   catch (Exception e) when(e is InvalidCastException || e is FormatException || e is OverflowException || e is ArgumentException)
//   validationErrors.Add($"Value {request.QueryStringParameters["x"]} at 'x' failed to satisfy constraint: {e.Message}");

    // TODO custom
    public static T BindValue<T>(IDictionary<string, string> parameter, string key, List<string> errors)
    {
        if (!parameter.TryGetValue(key, out var value))
        {
            errors.Add("Type convert failed");
            return default!;
        }

        // TODO Try
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    // TODO custom
    public static T BindValues<T>(IDictionary<string, IList<string>> parameter, string key, List<string> errors)
    {
        if (!parameter.TryGetValue(key, out var values))
        {
            errors.Add("Type convert failed");
            return default!;
        }

        // TODO Try
        return (T)Convert.ChangeType(values, typeof(T), CultureInfo.InvariantCulture);
    }
}
