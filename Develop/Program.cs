namespace Develop;

using AmazonLambdaExtension;

internal static class Program
{
    public static void Main()
    {
        Target.Method();
    }
}

internal static partial class Target
{
    [CustomMethod]
    public static partial void Method();
}
