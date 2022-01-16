namespace WorkGenerated;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

public static class Program
{
    public static void Main()
    {
        var value = new Input();
        var context = new ValidationContext(value);
        var results = new List<ValidationResult>();
        var ret = Validator.TryValidateObject(value, context, results, true);
        // TODO Annotation
    }
}

public class FunctionTestGenerated
{
    private readonly ServiceLocator serviceLocator;

    private readonly Function function;

    public FunctionTestGenerated()
    {
        serviceLocator = new ServiceLocator();

        var p0 = serviceLocator.CreateLogger<Function>();
        function = new Function(p0);
    }

    public async ValueTask<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
    {
        // TODO live header, 大文字小文字, X-?
        if (request.Headers.ContainsKey("X-Lambda-Keep-Alive"))
        {
            return new APIGatewayProxyResponse { StatusCode = 200 };
        }

        try
        {
            using var cts = new CancellationTokenSource(context.RemainingTime);

            // TODO serialize & TODO validation(message?)
            // TODO parse & validation

            var p4 = serviceLocator.ResolveService();
            var p5 = cts.Token;

            var result = await function.Test(null!, 0, 0, p4, p5);
            if (result is null)
            {
                return new APIGatewayProxyResponse { StatusCode = 404 };
            }

            // TODO serialize

            return new APIGatewayProxyResponse { StatusCode = 200 };

            // TODO API Exception
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return new APIGatewayProxyResponse { StatusCode = 500 };
        }
    }
}

public class Function
{
    private readonly Logger<Function> logger;

    public Function(Logger<Function> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<Output?> Test(Input input, int x, int y, Service service, CancellationToken cancellation)
    {
        logger.Log();

        await Task.Delay(0, cancellation);

        return new Output { Value = input.Value + service.Calc(x, y) };
    }
}

public class Input
{
    public int Value { get; set; }

    [Required]
    public int? Value2 { get; set; }
}

public class Output
{
    public int Value { get; set; }
}

public class ServiceLocator
{
    public Logger<T> CreateLogger<T>() => new();

    public Service ResolveService() => new();
}

public class Service
{
    public int Calc(int x, int y) => x + y;
}

public class Logger<T>
{
    public Type Type => typeof(T);

    public void Log()
    {
    }
}
