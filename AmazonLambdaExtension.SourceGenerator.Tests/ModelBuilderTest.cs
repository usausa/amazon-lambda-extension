namespace AmazonLambdaExtension.SourceGenerator.Tests;

using Xunit;

public class ModelBuilderTest
{
    [Fact]
    public void Test1()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

public class Input
{
    public string Value { get; set; }
}

public class Output
{
    public string Value { get; set; }
}

[Lambda]
public class Function
{
    [HttpApi]
    public Output Handle([FromBody] Input input)
    {
        return new Output { Value = input.Value };
    }
}
");

        var function = ModelBuilder.BuildFunctionInfo(info.Class);

        // Assert function
        Assert.Equal("Test.Function", function.Function.FullName);
        Assert.Empty(function.ConstructorParameters);
        Assert.Null(function.ServiceLocator);

        // TODO Assert handler
    }
}
