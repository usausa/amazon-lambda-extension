namespace WorkTemplate;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

using Amazon.Lambda.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using WorkTemplate.Models;
using WorkTemplate.Templates;

public static class Program
{
    public static void Main()
    {
        var lambdaModel = new LambdaModel
        {
            ContainingNamespace = "WorkTemplate",
            ConstructorParameters = new List<TypeModel>
            {
                new TypeModel { FullName = typeof(Service).FullName }
            },
            ServiceLocator = new TypeModel { FullName = typeof(ServiceLocator).FullName },
            Function = new TypeModel { FullName = typeof(Function).FullName }
        };
        var methodModel = new MethodModel
        {
            WrapperClassName = "Function_Method",
            IsAsync = true,
            Name = "Test",
            Parameters = new[]
            {
                new ParameterModel
                {
                    ParameterType = ParameterType.FromBody, Type = new TypeModel { FullName = typeof(Input).FullName }
                },
                new ParameterModel
                {
                    ParameterType = ParameterType.FromQuery, Type = new TypeModel { FullName = typeof(int).FullName }, Name = "x"
                },
                new ParameterModel
                {
                    ParameterType = ParameterType.FromRoute, Type = new TypeModel { FullName = typeof(int).FullName }, Name = "id"
                },
                new ParameterModel
                {
                    ParameterType = ParameterType.FromService, Type = new TypeModel { FullName = typeof(Service).FullName }
                },
                new ParameterModel
                {
                    ParameterType = ParameterType.None, Type = new TypeModel { FullName = typeof(ILambdaContext).FullName }
                }
            },
            ResultType = new TypeModel { FullName = typeof(Output).FullName }
        };

        var template = new LambdaTemplate(lambdaModel, methodModel);
        var sourceText = template.TransformText();

        Debug.WriteLine("--");
        Debug.WriteLine(sourceText);
        Debug.WriteLine("--");
    }
}

public class Function
{
    private readonly ILogger<Function> logger;

    public Function(ILogger<Function> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<Output?> Test(Input input, int x, int y, Service service, ILambdaContext context)
    {
        logger.LogInformation("test");

        using var tcs = new CancellationTokenSource(context.RemainingTime);
        await Task.Delay(0, tcs.Token);

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
    public ILogger<T> CreateLogger<T>() => new NullLogger<T>();

    public Service ResolveService() => new();
}

public class Service
{
    public int Calc(int x, int y) => x + y;
}
