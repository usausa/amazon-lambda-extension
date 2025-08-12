namespace AmazonLambdaExtension.Tests;

using AmazonLambdaExtension.Generator.Models;

public sealed class ModelBuilderTest
{
    // --------------------------------------------------------------------------------
    // Member
    // --------------------------------------------------------------------------------

    [Fact]
    public void TestNoServiceResolver()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public string Handle()
    {
        return ""OK"";
    }
}
");
        var function = ModelBuilder.BuildFunctionInfo(info.Class);

        // Assert
        Assert.Null(function.ServiceResolver);
    }

    [Fact]
    public void TestNoSerializer()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle([FromQuery] string input)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.False(handler.IsSerializerRequired());
    }

    // --------------------------------------------------------------------------------
    // Result
    // --------------------------------------------------------------------------------

    [Fact]
    public void TestResultVoid()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle()
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.False(handler.IsAsync);
        Assert.Null(handler.ResultType);
    }

    [Fact]
    public void TestResultVoidTask()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using System.Threading.Tasks;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public async Task Handle()
    {
        await Task.Delay(0);
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.True(handler.IsAsync);
        Assert.Null(handler.ResultType);
    }

    [Fact]
    public void TestResultVoidValueTask()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using System.Threading.Tasks;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public async ValueTask Handle()
    {
        await Task.Delay(0);
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.True(handler.IsAsync);
        Assert.Null(handler.ResultType);
    }

    [Fact]
    public void TestResultTask()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using System.Threading.Tasks;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public async Task<string> Handle()
    {
        await Task.Delay(0);
        return null;
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.True(handler.IsAsync);
        Assert.NotNull(handler.ResultType);
    }

    [Fact]
    public void TestResultValueTask()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using System.Threading.Tasks;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public async ValueTask<string> Handle()
    {
        await Task.Delay(0);
        return null;
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.True(handler.IsAsync);
        Assert.NotNull(handler.ResultType);
    }

    // --------------------------------------------------------------------------------
    // Parameter
    // --------------------------------------------------------------------------------

    [Fact]
    public void TestFromBody()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

public sealed class Input
{
    public string Value { get; set; }
}

public sealed class Output
{
    public string Value { get; set; }
}

[Lambda]
public sealed class Function
{
    [Api]
    public Output Handle([FromBody] Input input)
    {
        return new Output { Value = input.Value };
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Single(handler.Parameters);
        Assert.Equal(ParameterType.FromBody, handler.Parameters[0].ParameterType);
    }

    [Fact]
    public void TestFromQuery()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle([FromQuery] string a, [FromQuery] string[] b)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Equal(2, handler.Parameters.Count);
        Assert.Equal(ParameterType.FromQuery, handler.Parameters[0].ParameterType);
        Assert.False(handler.Parameters[0].Type.IsArrayType);
        Assert.Equal(ParameterType.FromQuery, handler.Parameters[1].ParameterType);
        Assert.True(handler.Parameters[1].Type.IsArrayType);
    }

    [Fact]
    public void TestFromQuery2()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle(string a, string[] b)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Equal(2, handler.Parameters.Count);
        Assert.Equal(ParameterType.FromQuery, handler.Parameters[0].ParameterType);
        Assert.False(handler.Parameters[0].Type.IsArrayType);
        Assert.Equal(ParameterType.FromQuery, handler.Parameters[1].ParameterType);
        Assert.True(handler.Parameters[1].Type.IsArrayType);
    }

    [Fact]
    public void TestFromRoute()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle([FromRoute] string id)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Single(handler.Parameters);
        Assert.Equal(ParameterType.FromRoute, handler.Parameters[0].ParameterType);
    }

    [Fact]
    public void TestFromHeader()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle([FromHeader] string a, [FromHeader] string[] b)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Equal(2, handler.Parameters.Count);
        Assert.Equal(ParameterType.FromHeader, handler.Parameters[0].ParameterType);
        Assert.False(handler.Parameters[0].Type.IsArrayType);
        Assert.Equal(ParameterType.FromHeader, handler.Parameters[1].ParameterType);
        Assert.True(handler.Parameters[1].Type.IsArrayType);
    }

    [Fact]
    public void TestFromServices()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using AmazonLambdaExtension.Annotations;

public interface IService
{
}

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle([FromServices] IService service)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Single(handler.Parameters);
        Assert.Equal(ParameterType.FromServices, handler.Parameters[0].ParameterType);
        Assert.Equal("Test.IService", handler.Parameters[0].Type.FullName);
    }

    [Fact]
    public void TestFromNone()
    {
        var info = BuilderInfo.Create(@"
namespace Test;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using AmazonLambdaExtension.Annotations;

[Lambda]
public sealed class Function
{
    [Api]
    public void Handle(APIGatewayProxyRequest request, ILambdaContext context)
    {
    }
}
");
        var handler = ModelBuilder.BuildHandlerInfo(info.Method);

        // Assert
        Assert.Equal(2, handler.Parameters.Count);
        Assert.Equal(ParameterType.None, handler.Parameters[0].ParameterType);
        Assert.Equal(ParameterType.None, handler.Parameters[1].ParameterType);
    }
}
