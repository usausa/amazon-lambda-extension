namespace AmazonLambdaExtension.Binders;

public enum BindingErrorKind
{
    // The value could not be converted to the parameter type
    InvalidValue,

    // The body is not valid JSON for the parameter type
    InvalidBody,

    // The body is required but missing
    MissingBody,

    // The body failed IRequestValidator
    ValidationFailed
}
