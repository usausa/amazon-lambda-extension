namespace AmazonLambdaExtension;

public sealed class EntryPointTests(ITestOutputHelper output)
{
    private const string EntryPointClassName = "AmazonLambdaExtension.Generated.LambdaEntryPoint";

    private const string EntryPoint =
        """
        [assembly: AmazonLambdaExtension.Annotations.LambdaEntryPoint]
        [assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

        """;

    private const string Functions =
        """
        namespace Test;

        using System.Threading.Tasks;
        using Amazon.Lambda.SQSEvents;
        using AmazonLambdaExtension.Annotations;
        using AmazonLambdaExtension.APIGateway;

        [Lambda]
        public sealed partial class ItemFunction
        {
            [HttpApi(LambdaHttpMethod.Get, "/items")]
            public IHttpResult List() => HttpResults.Ok();

            [HttpApiAuthorizer]
            public IAuthorizerResult Authorize() => AuthorizerResults.Allow();
        }

        [Lambda]
        public sealed partial class QueueFunction
        {
            [Event]
            public Task Handle(SQSEvent ev) => Task.CompletedTask;

            [Event]
            public SQSBatchResponse HandleBatch(SQSEvent ev) => new();
        }
        """;

    //--------------------------------------------------------------------------------
    // Generation
    //--------------------------------------------------------------------------------

    [Fact]
    public void WhenNoEntryPointAttributeMainIsNotGenerated()
    {
        var result = CompilationHelper.RunEntryPointGenerator(Functions);
        CompilationHelper.AssertNoGeneratorErrors(result);

        Assert.DoesNotContain(result.Sources.Values, static s => s.Contains("class LambdaEntryPoint", StringComparison.Ordinal));
    }

    [Fact]
    public void EntryPointDispatchesEachHandlerByHandlerName()
    {
        var result = CompilationHelper.RunEntryPointGenerator(EntryPoint + Functions);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var source = result.Sources.Values.Single(static s => s.Contains("class LambdaEntryPoint", StringComparison.Ordinal));
        output.WriteLine(source);

        Assert.Contains("global::System.Environment.GetEnvironmentVariable(\"_HANDLER\")", source, StringComparison.Ordinal);
        Assert.Contains("new global::Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer()", source, StringComparison.Ordinal);

        Assert.Contains("case \"TestAssembly::Test.ItemFunction::List_Handler\":", source, StringComparison.Ordinal);
        Assert.Contains(
            "RunAsync<global::Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest, global::Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyResponse>(global::Test.ItemFunction.List_Handler, serializer)",
            source,
            StringComparison.Ordinal);

        Assert.Contains("case \"TestAssembly::Test.ItemFunction::Authorize_Handler\":", source, StringComparison.Ordinal);
        Assert.Contains(
            "RunAsync<global::Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerV2Request, global::Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerV2SimpleResponse>(global::Test.ItemFunction.Authorize_Handler, serializer)",
            source,
            StringComparison.Ordinal);

        Assert.Contains("case \"TestAssembly::Test.QueueFunction::Handle_Handler\":", source, StringComparison.Ordinal);
        Assert.Contains(
            "RunAsync<global::Amazon.Lambda.SQSEvents.SQSEvent>(global::Test.QueueFunction.Handle_Handler, serializer)",
            source,
            StringComparison.Ordinal);

        Assert.Contains("case \"TestAssembly::Test.QueueFunction::HandleBatch_Handler\":", source, StringComparison.Ordinal);
        Assert.Contains(
            "RunAsync<global::Amazon.Lambda.SQSEvents.SQSEvent, global::Amazon.Lambda.SQSEvents.SQSBatchResponse>(global::Test.QueueFunction.HandleBatch_Handler, serializer)",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EntryPointOrdersHandlersByHandlerName()
    {
        var result = CompilationHelper.RunEntryPointGenerator(EntryPoint + Functions);
        var source = result.Sources.Values.Single(static s => s.Contains("class LambdaEntryPoint", StringComparison.Ordinal));

        var names = new[]
        {
            "TestAssembly::Test.ItemFunction::Authorize_Handler",
            "TestAssembly::Test.ItemFunction::List_Handler",
            "TestAssembly::Test.QueueFunction::HandleBatch_Handler",
            "TestAssembly::Test.QueueFunction::Handle_Handler"
        };
        var positions = names.Select(x => source.IndexOf($"case \"{x}\":", StringComparison.Ordinal)).ToArray();

        Assert.All(positions, static x => Assert.True(x >= 0));
        Assert.Equal(positions.Order().ToArray(), positions);
    }

    [Fact]
    public void EntryPointWithoutResultlessHandlerOmitsUnusedRunMethod()
    {
        var result = CompilationHelper.RunEntryPointGenerator(
            EntryPoint +
            """
            namespace Test;

            using AmazonLambdaExtension.Annotations;
            using AmazonLambdaExtension.APIGateway;

            [Lambda]
            public sealed partial class Function
            {
                [HttpApi(LambdaHttpMethod.Get, "/items")]
                public IHttpResult List() => HttpResults.Ok();
            }
            """);
        CompilationHelper.AssertNoGeneratorErrors(result);
        var source = result.Sources.Values.Single(static s => s.Contains("class LambdaEntryPoint", StringComparison.Ordinal));
        output.WriteLine(source);

        Assert.Contains("RunAsync<TInput, TOutput>(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RunAsync<TInput>(", source, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // Runtime
    //--------------------------------------------------------------------------------

    [Fact]
    public void EntryPointBecomesMainOfExecutable()
    {
        var assembly = CompilationHelper.LoadEntryPointAssembly(EntryPoint + Functions);

        Assert.Equal(EntryPointClassName, assembly.EntryPoint?.DeclaringType?.FullName);
    }

    [Fact]
    public void EntryPointWithoutHandlersCompiles()
    {
        var assembly = CompilationHelper.LoadEntryPointAssembly(
            EntryPoint +
            """
            namespace Test;

            public sealed class Placeholder
            {
            }
            """);

        Assert.Equal(EntryPointClassName, assembly.EntryPoint?.DeclaringType?.FullName);
    }

    [Fact]
    public async Task MainThrowsWhenHandlerIsUnknown()
    {
        const string handler = "TestAssembly::Test.ItemFunction::Unknown_Handler";
        var assembly = CompilationHelper.LoadEntryPointAssembly(EntryPoint + Functions);
        var main = assembly.GetType(EntryPointClassName, throwOnError: true)!.GetMethod("Main")!;

        var previous = Environment.GetEnvironmentVariable("_HANDLER");
        Environment.SetEnvironmentVariable("_HANDLER", handler);
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => (Task)main.Invoke(null, null)!);
            Assert.Contains(handler, exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("_HANDLER", previous);
        }
    }
}
