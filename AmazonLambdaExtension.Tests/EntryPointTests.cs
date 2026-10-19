namespace AmazonLambdaExtension;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

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
        var previousApi = Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API");
        Environment.SetEnvironmentVariable("_HANDLER", handler);
        Environment.SetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API", null);
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => (Task)main.Invoke(null, null)!);
            Assert.Contains(handler, exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("_HANDLER", previous);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API", previousApi);
        }
    }

    [Fact(Timeout = 10000)]
    public async Task MainReportsInitErrorWhenHandlerIsUnknown()
    {
        const string handler = "TestAssembly::Test.ItemFunction::Unknown_Handler";
        var assembly = CompilationHelper.LoadEntryPointAssembly(EntryPoint + Functions);
        var main = assembly.GetType(EntryPointClassName, throwOnError: true)!.GetMethod("Main")!;

        var (exception, requestLine, body) = await InvokeWithRuntimeApiAsync(main, handler, TestContext.Current.CancellationToken);
        using var error = JsonDocument.Parse(body);

        Assert.Contains(handler, Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal);
        Assert.Equal("POST /2018-06-01/runtime/init/error HTTP/1.1", requestLine);
        Assert.Equal("InvalidOperationException", error.RootElement.GetProperty("errorType").GetString());
        Assert.Contains(handler, error.RootElement.GetProperty("errorMessage").GetString(), StringComparison.Ordinal);
    }

    private static async Task<(Exception? Exception, string RequestLine, string Body)> InvokeWithRuntimeApiAsync(MethodInfo main, string handler, CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = ServeRuntimeApiAsync(started, cancellationToken);
        var port = await started.Task.ConfigureAwait(false);

        var previous = Environment.GetEnvironmentVariable("_HANDLER");
        var previousApi = Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API");
        var previousCapture = Environment.GetEnvironmentVariable("AWS_LAMBDA_DOTNET_DISABLE_CONSOLE_CAPTURE");
        Environment.SetEnvironmentVariable("_HANDLER", handler);
        Environment.SetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API", $"127.0.0.1:{port}");
        Environment.SetEnvironmentVariable("AWS_LAMBDA_DOTNET_DISABLE_CONSOLE_CAPTURE", "true");
        try
        {
            var exception = await Record.ExceptionAsync(() => (Task)main.Invoke(null, null)!).ConfigureAwait(false);
            var (requestLine, body) = await received.ConfigureAwait(false);
            return (exception, requestLine, body);
        }
        finally
        {
            Environment.SetEnvironmentVariable("_HANDLER", previous);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API", previousApi);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_DOTNET_DISABLE_CONSOLE_CAPTURE", previousCapture);
        }
    }

    private static async Task<(string RequestLine, string Body)> ServeRuntimeApiAsync(TaskCompletionSource<int> started, CancellationToken cancellationToken)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        started.SetResult(((IPEndPoint)listener.LocalEndpoint).Port);

        using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var contentLength = 0;
        string? line;
        while (!String.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                contentLength = Int32.Parse(line["Content-Length:".Length..].Trim(), CultureInfo.InvariantCulture);
            }
        }

        var body = new char[contentLength];
        await reader.ReadBlockAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync("HTTP/1.1 202 Accepted\r\nContent-Type: application/json\r\nContent-Length: 15\r\nConnection: close\r\n\r\n{\"status\":\"OK\"}"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        return (requestLine, new string(body));
    }
}
