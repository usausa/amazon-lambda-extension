namespace AmazonLambdaExtension.Helpers;

using System.ComponentModel.DataAnnotations;

public static class ValidationHelper
{
    public static bool Validate(object value)
    {
        var context = new ValidationContext(value);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(value, context, results, true);
    }
}
