namespace AmazonLambdaExtension;

#pragma warning disable CA1032
public sealed class ApiException : Exception
{
    public int StatusCode { get; set; }

    public ApiException(int statusCode)
    {
        StatusCode = statusCode;
    }
}
#pragma warning restore CA1032
