namespace AmazonLambdaExtension;

public sealed class GeneratedCodeTests
{
    [Theory]
    [InlineData("[FromQuery] int?[] ids")]
    [InlineData("[FromQuery] Mode?[] modes")]
    [InlineData("[FromQuery] string?[] names")]
    [InlineData("[FromQuery] string? keyword")]
    [InlineData("[FromQuery] Mode mode = (Mode)(-1)")]
    [InlineData("[FromQuery] double rate = double.PositiveInfinity")]
    [InlineData("[FromQuery] decimal price = 0.1m")]
    [InlineData("[FromQuery(\"{page}\")] int page")]
    [InlineData("[FromHeader(\"x-\\\"q\\\"\")] string q")]
    [InlineData("[FromAuthorizer(\"level\")] Mode? level")]
    public void ParameterBindingCompiles(string parameter)
    {
        var source = $$"""
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public enum Mode { Basic, Advanced }

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get({{parameter}}) => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void KeywordHandlerNameCompiles()
    {
        const string source =
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public sealed class MyEvent { }

            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public void @event(MyEvent ev)
                {
                }

                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult @class() => HttpResults.Ok();
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0027HandlersDifferingOnlyInCaseGenerateTheFirstOnly()
    {
        const string source =
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();

                [HttpApi(LambdaHttpMethod.Get, "/b")]
                public IHttpResult get() => HttpResults.Ok();
            }
            """;

        var sources = CompilationHelper.RunGenerator(source).Sources.Values.ToList();

        Assert.Equal(["ALE0027"], CompilationHelper.GetProblemIds(source));
        Assert.Contains(sources, static x => x.Contains("Get_Handler(", StringComparison.Ordinal));
        Assert.DoesNotContain(sources, static x => x.Contains("get_Handler(", StringComparison.Ordinal));
    }

    [Fact]
    public void NullableAnnotationsOfEventHandlerAreKept()
    {
        const string source =
            """
            namespace Test;

            using System.Collections.Generic;
            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.Filters;

            public sealed class PassFilter : ILambdaFilter
            {
                public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
            }

            [Lambda]
            public sealed partial class Function
            {
                [Event]
                public string? Handle(List<string?> ev) => ev[0];
            }

            [Lambda]
            [Filter<PassFilter>]
            public sealed partial class FilteredFunction
            {
                [Event]
                public Task<List<string?>> Handle(List<string?> ev) => Task.FromResult(ev);
            }
            """;

        Assert.Empty(CompilationHelper.GetProblemIds(source));
    }

    [Fact]
    public void Ale0027ClassNamesDifferingOnlyInCaseGenerateTheFirstOnly()
    {
        const string source =
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();
            }

            [Lambda]
            public sealed partial class function
            {
                [HttpApi(LambdaHttpMethod.Get, "/b")]
                public IHttpResult Get() => HttpResults.Ok();
            }
            """;

        var problems = CompilationHelper.GetProblemIds(source);

        Assert.Contains("ALE0027", problems);
        Assert.DoesNotContain("CS8785", problems);
    }

    [Fact]
    public void Ale0028BaseClassHandlerIsReportedOnce()
    {
        const string source =
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            public abstract class FunctionBase
            {
                [HttpApi(LambdaHttpMethod.Get, "/base")]
                public IHttpResult Base() => HttpResults.Ok();
            }

            [Lambda]
            public sealed partial class FirstFunction : FunctionBase
            {
                [HttpApi(LambdaHttpMethod.Get, "/first")]
                public IHttpResult First() => HttpResults.Ok();
            }

            [Lambda]
            public sealed partial class SecondFunction : FunctionBase
            {
                [HttpApi(LambdaHttpMethod.Get, "/second")]
                public IHttpResult Second() => HttpResults.Ok();
            }
            """;

        Assert.Equal(["ALE0028"], CompilationHelper.GetProblemIds(source));
    }

    [Theory]
    [InlineData("public required string Name { get; init; }", "")]
    [InlineData("public required string Name { get; init; }", "[System.Diagnostics.CodeAnalysis.SetsRequiredMembers] public Function() { Name = string.Empty; }")]
    public void Ale0026RequiredMembersNeedSetsRequiredMembers(string member, string constructor)
    {
        var source = $$"""
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                {{member}}

                {{constructor}}

                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();
            }
            """;

        var problems = CompilationHelper.GetProblemIds(source);

        if (constructor.Length == 0)
        {
            Assert.Contains("ALE0026", problems);
        }
        else
        {
            Assert.Empty(problems);
        }
    }

    [Fact]
    public void Ale0003FileLocalClassEmitsDiagnostic()
    {
        const string source =
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            file sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();
            }
            """;

        Assert.Contains("ALE0003", CompilationHelper.GetProblemIds(source));
    }
}
