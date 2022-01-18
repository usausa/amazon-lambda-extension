namespace AmazonLambdaExtension.TargetProject;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using AmazonLambdaExtension.Annotations;

using Microsoft.Extensions.Logging;

#pragma warning disable CA1822

public sealed class ServiceLocator
{
    //private readonly ILoggerFactory loggerFactory = new LambdaLoggerFactory(LogLevel.Information, null);

    //public void Dispose()
    //{
    //    loggerFactory.Dispose();
    //}

    //public ILogger<T> CreateLogger<T>() => loggerFactory.CreateLogger<T>();

    //public static ICalculator ResolveCalculator() => new Calculator();

    //// TODO
    //public IBodySerializer ResolveSerializer() => JsonBodySerializer.Default;

    // TODO
    public T GetService<T>() => default!;
}

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
[ServiceResolver(typeof(ServiceLocator))]
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
}

[Lambda]
[ServiceResolver(typeof(ServiceLocator))]
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
}
