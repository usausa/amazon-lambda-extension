namespace AmazonLambdaExtension.Validation;

public interface IRequestValidator
{
    bool Validate(object value);
}
