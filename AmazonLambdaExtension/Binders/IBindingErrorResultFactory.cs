namespace AmazonLambdaExtension.Binders;

using AmazonLambdaExtension.APIGateway;

// Creates the response when parameter binding or body validation fails.
// Register it as a singleton in ServiceResolver; without it, the response is a 400 with a text message.
public interface IBindingErrorResultFactory
{
    IHttpResult Create(BindingError bindingError);
}
