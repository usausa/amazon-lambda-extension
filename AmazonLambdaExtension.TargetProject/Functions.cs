namespace AmazonLambdaExtension.TargetProject;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using AmazonLambdaExtension.Annotations;

using Microsoft.Extensions.Logging;

#pragma warning disable CA1822

public sealed class ApplicationServiceResolver
{
    public T GetService<T>() => default!;
}

#pragma warning disable IDE0060
public sealed class ApplicationFilter
{
    public async Task<APIGatewayProxyResponse?> OnFunctionExecuting(ILambdaContext context)
    {
        await Task.Delay(0);
        return null;
    }

    public async Task OnFunctionExecuted()
    {
        await Task.Delay(0);
    }
}
#pragma warning restore IDE0060

public interface ICalculator
{
    public int Add(int x, int y);
}

public class Calculator : ICalculator
{
    public int Add(int x, int y) => x + y;
}

public class Input
{
    [Required]
    [AllowNull]
    public string Value { get; set; }
}

public class Output
{
    [AllowNull]
    public string Value { get; set; }
}

[Lambda]
[ServiceResolver(typeof(ApplicationServiceResolver))]
[Filter(typeof(ApplicationFilter))]
public class Function1
{
    private readonly ILogger<Function1> logger;

    public Function1(ILogger<Function1> logger)
    {
        this.logger = logger;
    }

    [HttpApi]
    public Output TestBody([FromBody] Input input)
    {
        return new Output { Value = input.Value };
    }

    [HttpApi]
    public void TestBodyVoid([FromBody] Input input)
    {
        logger.LogDebug("Value=[{Value}]", input.Value);
    }

    [HttpApi]
    public int TestQuery([FromQuery] int a, [FromQuery] int[] b)
    {
        return a + b.Length;
    }

    [HttpApi]
    public void TestRaw(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogLine(request.Path);
    }
}

[Lambda]
[ServiceResolver(typeof(ApplicationServiceResolver))]
[Filter(typeof(ApplicationFilter))]
public class Function2
{
    private readonly ICalculator calculator;

    public Function2(ICalculator calculator)
    {
        this.calculator = calculator;
    }

    [HttpApi]
    public int TestCalc([FromQuery] int x, [FromQuery] int y)
    {
        return calculator.Add(x, y);
    }

    [HttpApi]
    public int TestCalcNoAttribute(int x, int y)
    {
        return calculator.Add(x, y);
    }

    [HttpApi]
    public int TestCalcWithName([FromQuery("a")] int x, [FromQuery("b")] int y)
    {
        return calculator.Add(x, y);
    }
}

[Lambda]
public class Function3
{
    [HttpApi]
    public void TestVoid()
    {
    }

    [HttpApi]
    public async Task TestTask()
    {
        await Task.Delay(0).ConfigureAwait(false);
    }

    [HttpApi]
    public async Task<int> TestTask2()
    {
        await Task.Delay(0).ConfigureAwait(false);
        return 0;
    }

    [HttpApi]
    public async ValueTask TestValueTask()
    {
        await Task.Delay(0).ConfigureAwait(false);
    }

    [HttpApi]
    public async ValueTask<int> TestValueTask2()
    {
        await Task.Delay(0).ConfigureAwait(false);
        return 0;
    }
}
