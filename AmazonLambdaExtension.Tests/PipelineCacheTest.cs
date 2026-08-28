namespace AmazonLambdaExtension;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTest
{
    private const string Source =
        """
        namespace Test;

        using AmazonLambdaExtension.Annotations;

        public sealed class MyEvent { }

        [Lambda]
        public sealed partial class Function
        {
            [Event]
            public void Handle(MyEvent ev)
            {
            }
        }
        """;

    private const string UnrelatedSource =
        """
        namespace Other;

        internal sealed class Unrelated;
        """;

    private const string AddedTargetSource =
        """
        namespace Test;

        using AmazonLambdaExtension.Annotations;

        [Lambda]
        public sealed partial class AddedFunction
        {
            [Event]
            public void Handle(MyEvent ev)
            {
            }
        }
        """;

    // ------------------------------------------------------------
    // Cache
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsModelCached()
    {
        // Arrange & Act
        var result = CompilationHelper.RunIncremental(Source, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void TargetEditRebuildsModel()
    {
        // Arrange & Act
        var result = CompilationHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }
}
