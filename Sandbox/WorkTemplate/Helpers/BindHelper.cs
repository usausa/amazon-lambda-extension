namespace WorkTemplate.Helpers;

public static class BindHelper
{
    public static T BindValue<T>(IDictionary<string, string> values, string key, List<string> errors)
    {
        if (!values.TryGetValue(key, out var value))
        {
            errors.Add("Type convert failed");
            return default!;
        }

        // Try
        return (T)Convert.ChangeType(value, typeof(T));
    }
}
