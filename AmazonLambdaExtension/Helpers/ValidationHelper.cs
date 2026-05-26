namespace AmazonLambdaExtension.Helpers;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public static class ValidationHelper
{
    [RequiresUnreferencedCode("DataAnnotations validation uses reflection over instance properties.")]
    public static bool Validate(object value)
    {
        var context = new ValidationContext(value);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(value, context, results, true);
    }
}
