namespace AmazonLambdaExtension;

#pragma warning disable CA1032
public sealed class ApiException : Exception
{
    public int Code { get; set; }

    public ApiException(int code)
    {
        Code = code;
    }
}
#pragma warning restore CA1032
