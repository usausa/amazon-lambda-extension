namespace AmazonLambdaExtension.Annotations;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FunctionUrlAttribute : Attribute
{
    public FunctionUrlAuthType AuthType { get; set; }

    public string[]? AllowOrigins { get; set; }

    public string[]? AllowMethods { get; set; }

    public string[]? AllowHeaders { get; set; }

    public bool AllowCredentials { get; set; }

    public string[]? ExposeHeaders { get; set; }

    public int MaxAge { get; set; }
}
