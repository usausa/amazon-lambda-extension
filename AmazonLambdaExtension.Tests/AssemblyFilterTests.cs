namespace AmazonLambdaExtension;

using Amazon.Lambda.APIGatewayEvents;

public sealed class AssemblyFilterTests
{
    private const string Head =
        """
        namespace Test;

        using System.Collections.Generic;
        using System.Threading.Tasks;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;
        using AmazonLambdaExtension.Filters;

        public static class Trace
        {
            public static List<string> Items { get; } = [];

            public static string Take()
            {
                var value = string.Join(",", Items);
                Items.Clear();
                return value;
            }
        }

        public sealed class AFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
            {
                Trace.Items.Add("A");
                return next(context);
            }
        }

        public sealed class BFilter : ILambdaFilter
        {
            public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next)
            {
                Trace.Items.Add("B");
                return next(context);
            }
        }

        """;

    private static string Functions(string classFilter) =>
        $$"""
        [Lambda]
        {{classFilter}}
        public sealed partial class Function
        {
            [HttpApi(LambdaHttpMethod.Get, "/a")]
            public IHttpResult Get() => HttpResults.Ok(Trace.Take());
        }

        [Lambda]
        public sealed partial class OtherFunction
        {
            [HttpApi(LambdaHttpMethod.Get, "/b")]
            public IHttpResult Get() => HttpResults.Ok(Trace.Take());
        }
        """;

    [Fact]
    public async Task AssemblyFilterAppliesToEveryClass()
    {
        var assembly = CompilationHelper.LoadAssembly("[assembly: AmazonLambdaExtension.Annotations.Filter<Test.AFilter>]" + Environment.NewLine + Head + Functions(string.Empty));

        Assert.Equal("A", (await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest())).Body);
        Assert.Equal("A", (await InvokeOtherAsync(assembly)).Body);
    }

    [Fact]
    public async Task AssemblyFilterIsOuterWithSameOrder()
    {
        var assembly = CompilationHelper.LoadAssembly("[assembly: AmazonLambdaExtension.Annotations.Filter<Test.AFilter>]" + Environment.NewLine + Head + Functions("[Filter<BFilter>]"));

        Assert.Equal("A,B", (await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest())).Body);
        Assert.Equal("A", (await InvokeOtherAsync(assembly)).Body);
    }

    [Fact]
    public async Task OrderIsSharedBetweenAssemblyAndClass()
    {
        var assembly = CompilationHelper.LoadAssembly("[assembly: AmazonLambdaExtension.Annotations.Filter<Test.AFilter>(Order = 10)]" + Environment.NewLine + Head + Functions("[Filter<BFilter>(Order = 0)]"));

        Assert.Equal("B,A", (await CompilationHelper.InvokeAsync(assembly, "Get_Handler", new APIGatewayHttpApiV2ProxyRequest())).Body);
    }

    [Fact]
    public void AssemblyFilterDiagnosticIsReportedOnce()
    {
        var diagnostics = CompilationHelper.RunGenerator(
            """
            [assembly: AmazonLambdaExtension.Annotations.Filter<Test.NoDefaultCtorFilter>]
            namespace Test;

            using System.Threading.Tasks;
            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;
            using AmazonLambdaExtension.Filters;

            public sealed class NoDefaultCtorFilter : ILambdaFilter
            {
                public NoDefaultCtorFilter(int value)
                {
                }

                public ValueTask InvokeAsync(LambdaInvocationContext context, LambdaFilterDelegate next) => next(context);
            }

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/a")]
                public IHttpResult Get() => HttpResults.Ok();
            }

            [Lambda]
            public sealed partial class OtherFunction
            {
                [HttpApi(LambdaHttpMethod.Get, "/b")]
                public IHttpResult Get() => HttpResults.Ok();
            }
            """).Diagnostics;

        var diagnostic = Assert.Single(diagnostics, static x => x.Id == "ALE0203");
        Assert.True(diagnostic.Location.IsInSource);
    }

    private static Task<APIGatewayHttpApiV2ProxyResponse> InvokeOtherAsync(System.Reflection.Assembly assembly)
    {
        var method = assembly.GetType("Test.OtherFunction", throwOnError: true)!.GetMethod("Get_Handler")!;
        return (Task<APIGatewayHttpApiV2ProxyResponse>)method.Invoke(null, [new APIGatewayHttpApiV2ProxyRequest(), new NullLambdaContext()])!;
    }

    private sealed class NullLambdaContext : Amazon.Lambda.Core.ILambdaContext
    {
        public string AwsRequestId => "request";

        public Amazon.Lambda.Core.IClientContext ClientContext => null!;

        public string FunctionName => "function";

        public string FunctionVersion => "1";

        public Amazon.Lambda.Core.ICognitoIdentity Identity => null!;

        public string InvokedFunctionArn => "arn";

        public Amazon.Lambda.Core.ILambdaLogger Logger => null!;

        public string LogGroupName => "group";

        public string LogStreamName => "stream";

        public int MemoryLimitInMB => 128;

        public TimeSpan RemainingTime => TimeSpan.FromMinutes(1);
    }
}
