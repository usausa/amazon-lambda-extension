# AWS Lambda Extension for .NET

[![NuGet Badge](https://buildstats.info/nuget/AmazonLambdaExtension)](https://www.nuget.org/packages/AmazonLambdaExtension/)
[![NuGet Badge](https://buildstats.info/nuget/AmazonLambdaExtension.SourceGenerator)](https://www.nuget.org/packages/AmazonLambdaExtension.SourceGenerator/)

## What is this?

Source Generator for AWS Lambda HTTP API inspired by [Amazon.Lambda.Annotations](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.Annotations/README.md).

## Supported features

* Dependency Injection support
* Parameter binding
* Pre/Post action
* Auto validation and return 400
* Return 404 if response is null

## Usage

Add reference to AmazonLambdaExtension and AmazonLambdaExtension.SourceGenerator .

```xml
<ItemGroup>
  <PackageReference Include="AmazonLambdaExtension" Version="1.2.0" />
  <PackageReference Include="AmazonLambdaExtension.SourceGenerator" Version="1.2.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Change Handler in serverless.template to generated code.

* Before

```yaml
"CrudGet": {
  "Type": "AWS::Serverless::Function",
  "Properties": {
    "Handler": "AmazonLambdaExtension.Example::AmazonLambdaExtension.Example.Functions.CrudFunction::Get",
...
```

* After

```yaml
"CrudGet": {
  "Type": "AWS::Serverless::Function",
  "Properties": {
    "Handler": "AmazonLambdaExtension.Example::AmazonLambdaExtension.Example.Functions.CrudFunction_Get::Handle",
...
```

## Basic

Set `LambdaAttribute` to Function class.  
Set `ApiAttribute` to method to handle HTTP API and `EventAttribute` for other.  
Then the wrapper class will be generated.

## Dependency Injection

Create class that has `GetService<T>()` method and set type to `ServiceResolverAttribute`.

```csharp
public sealed class ServiceResolver
{
    private readonly IServiceProvider provider = new ServiceCollection()
        .AddLogging(c =>
        {
            c.ClearProviders();
            c.AddLambdaLogger();
        })
        .BuildServiceProvider();

    public T GetService<T>() => provider.GetService<T>();
}
```

```csharp
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
public sealed class Function
{
    private readonly ILogger<Function> logger;

    public Function(ILogger<Function> logger)
    {
        this.logger = logger;
    }
...
}
```

By default, Function classes are directly new.
If set `ResolveFunction = true` in `ServiceResolverAttribute`, Function class will also be resolved by Dependency Injection.

## Pre/Post action

Create class with `OnFunctionExecuting()`/`OnFunctionExecuted()` methods and set type in `FilterAttribute`, method will be called before and after Function is processed.  
In the case of HTTP API, `APIGatewayProxyRequest` and `ILambdaContext` are arguments to method.  
In the case of Event, `ILambdaContext` is arguments to method.  
In the HTTP API filter, if `OnFunctionExecuting()` returns `APIGatewayProxyResponse`, the Function process will not be called and its value will be used as the response.  
Filter methods support `async`.

```csharp
public sealed class ApiFilter
{
    public APIGatewayProxyResponse OnFunctionExecuting(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.Headers?.ContainsKey("X-Lambda-Ping") ?? false)
        {
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 200 };
        }

        LambdaLoggerContext.RequestId = context.AwsRequestId;

        return null;
    }

    public void OnFunctionExecuted(APIGatewayProxyRequest request, ILambdaContext context)
    {
        LambdaLoggerContext.RequestId = null;
    }
}
```

```csharp
[Lambda]
[Filter(typeof(ApiFilter))]
public sealed class Function
{
...
}
```

## Parameter binding

| Attribute   | Source               | HTTP API | Event |
|:------------|:---------------------|:--------:|:-----:|
| FromHeader  | HTTP header          | ✅ |    |
| FromQuery   | Query string         | ✅ |    |
| FromRoute   | Path parameter       | ✅ |    |
| FromBody    | HTTP request body    | ✅ |    |
| FromService | Dependency Injection | ✅ | ✅ |

Objects with `FromBodyAttribute` is validated by DataAnnotations.

## Source generation

Samples of source and generated code.

### Source

```csharp
[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
[Filter(typeof(Filter))]
public class CrudFunction
{
    private readonly ILogger<CrudFunction> logger;

    private readonly IMapper mapper;

    private readonly DataService dataService;

    public CrudFunction(ILogger<CrudFunction> logger, IMapper mapper, DataService dataService)
    {
        this.logger = logger;
        this.mapper = mapper;
        this.dataService = dataService;
    }

    [Api]
    public async ValueTask<DataEntity?> Get([FromRoute] string id)
    {
        return await dataService.QueryDataAsync(id).ConfigureAwait(false);
    }

    [Api]
    public async ValueTask<CrudCreateOutput> Create([FromBody] CrudCreateInput input)
    {
        var entity = mapper.Map<DataEntity>(input);
        entity.Id = Guid.NewGuid().ToString();
        entity.CreatedAt = DateTime.Now;

        await dataService.CreateDataAsync(entity).ConfigureAwait(false);

        logger.LogInformation("Data created. id=[{Id}]", entity.Id);

        return new CrudCreateOutput { Id = entity.Id };
    }

    [Api]
    public async ValueTask Delete([FromRoute] string id)
    {
        await dataService.DeleteDataAsync(id).ConfigureAwait(false);

        logger.LogInformation("Data deleted. id=[{Id}]", id);
    }
}
```

### Generated code

```csharp
public sealed class CrudFunction_Get
{
    private readonly AmazonLambdaExtension.Example.ServiceResolver serviceResolver;

    private readonly AmazonLambdaExtension.Example.Filter filter;

    private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

    private readonly AmazonLambdaExtension.Example.Functions.CrudFunction function;

    public CrudFunction_Get()
    {
        serviceResolver = new AmazonLambdaExtension.Example.ServiceResolver();
        filter = new AmazonLambdaExtension.Example.Filter();
        serializer = serviceResolver.GetService<AmazonLambdaExtension.Serialization.IBodySerializer>() ?? AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
        function = new AmazonLambdaExtension.Example.Functions.CrudFunction(serviceResolver.GetService<Microsoft.Extensions.Logging.ILogger<AmazonLambdaExtension.Example.Functions.CrudFunction>>(),serviceResolver.GetService<AutoMapper.IMapper>(),serviceResolver.GetService<AmazonLambdaExtension.Example.Services.DataService>());
    }

    public async System.Threading.Tasks.Task<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
    {
        var executingResult = filter.OnFunctionExecuting(request, context);
        if (executingResult != null)
        {
            return executingResult;
        }

        try
        {
            if (!AmazonLambdaExtension.Helpers.BindHelper.TryBind<string>(request.PathParameters, "id", out var p0))
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
            }

            var output = await function.Get(p0).ConfigureAwait(false);
            if (output == null)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 404 };
            }

            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse
            {
                Body = serializer.Serialize(output),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                StatusCode = 200
            };
        }
        catch (AmazonLambdaExtension.ApiException ex)
        {
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = ex.StatusCode };
        }
        catch (System.Exception ex)
        {
            context.Logger.LogLine(ex.ToString());
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 500 };
        }
        finally
        {
            filter.OnFunctionExecuted(request, context);
        }
    }
}
```

```csharp
public sealed class CrudFunction_Create
{
    private readonly AmazonLambdaExtension.Example.ServiceResolver serviceResolver;

    private readonly AmazonLambdaExtension.Example.Filter filter;

    private readonly AmazonLambdaExtension.Serialization.IBodySerializer serializer;

    private readonly AmazonLambdaExtension.Example.Functions.CrudFunction function;

    public CrudFunction_Create()
    {
        serviceResolver = new AmazonLambdaExtension.Example.ServiceResolver();
        filter = new AmazonLambdaExtension.Example.Filter();
        serializer = serviceResolver.GetService<AmazonLambdaExtension.Serialization.IBodySerializer>() ?? AmazonLambdaExtension.Serialization.JsonBodySerializer.Default;
        function = new AmazonLambdaExtension.Example.Functions.CrudFunction(serviceResolver.GetService<Microsoft.Extensions.Logging.ILogger<AmazonLambdaExtension.Example.Functions.CrudFunction>>(),serviceResolver.GetService<AutoMapper.IMapper>(),serviceResolver.GetService<AmazonLambdaExtension.Example.Services.DataService>());
    }

    public async System.Threading.Tasks.Task<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
    {
        var executingResult = filter.OnFunctionExecuting(request, context);
        if (executingResult != null)
        {
            return executingResult;
        }

        try
        {
            AmazonLambdaExtension.Example.Parameters.CrudCreateInput p0;
            try
            {
                p0 = serializer.Deserialize<AmazonLambdaExtension.Example.Parameters.CrudCreateInput>(request.Body);
            }
            catch (System.Exception ex)
            {
                context.Logger.LogLine(ex.ToString());
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
            }

            if (!AmazonLambdaExtension.Helpers.ValidationHelper.Validate(p0))
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
            }

            var output = await function.Create(p0).ConfigureAwait(false);
            if (output == null)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 404 };
            }

            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse
            {
                Body = serializer.Serialize(output),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                StatusCode = 200
            };
        }
        catch (AmazonLambdaExtension.ApiException ex)
        {
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = ex.StatusCode };
        }
        catch (System.Exception ex)
        {
            context.Logger.LogLine(ex.ToString());
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 500 };
        }
        finally
        {
            filter.OnFunctionExecuted(request, context);
        }
    }
}
```

```csharp
public sealed class CrudFunction_Delete
{
    private readonly AmazonLambdaExtension.Example.ServiceResolver serviceResolver;

    private readonly AmazonLambdaExtension.Example.Filter filter;

    private readonly AmazonLambdaExtension.Example.Functions.CrudFunction function;

    public CrudFunction_Delete()
    {
        serviceResolver = new AmazonLambdaExtension.Example.ServiceResolver();
        filter = new AmazonLambdaExtension.Example.Filter();
        function = new AmazonLambdaExtension.Example.Functions.CrudFunction(serviceResolver.GetService<Microsoft.Extensions.Logging.ILogger<AmazonLambdaExtension.Example.Functions.CrudFunction>>(),serviceResolver.GetService<AutoMapper.IMapper>(),serviceResolver.GetService<AmazonLambdaExtension.Example.Services.DataService>());
    }

    public async System.Threading.Tasks.Task<Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.ILambdaContext context)
    {
        var executingResult = filter.OnFunctionExecuting(request, context);
        if (executingResult != null)
        {
            return executingResult;
        }

        try
        {
            if (!AmazonLambdaExtension.Helpers.BindHelper.TryBind<string>(request.PathParameters, "id", out var p0))
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 400 };
            }

            await function.Delete(p0).ConfigureAwait(false);

            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse
            {
                StatusCode = 200
            };
        }
        catch (AmazonLambdaExtension.ApiException ex)
        {
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = ex.StatusCode };
        }
        catch (System.Exception ex)
        {
            context.Logger.LogLine(ex.ToString());
            return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 500 };
        }
        finally
        {
            filter.OnFunctionExecuted(request, context);
        }
    }
}
```
