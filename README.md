# AWS Lambda Extension for .NET

[![NuGet Badge](https://buildstats.info/nuget/AmazonLambdaExtension)](https://www.nuget.org/packages/AmazonLambdaExtension/)
[![NuGet Badge](https://buildstats.info/nuget/AmazonLambdaExtension.SourceGenerator)](https://www.nuget.org/packages/AmazonLambdaExtension.SourceGenerator/)

## What is this?

* Source Generator for AWS Lambda HTTP API inspired by [Amazon.Lambda.Annotations](https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.Annotations/README.md).

## Usage

Add reference to AmazonLambdaExtension and AmazonLambdaExtension.SourceGenerator .

```xml
<ItemGroup>
<PackageReference Include="AmazonLambdaExtension" Version="1.0.0-beta1" />
<PackageReference Include="AmazonLambdaExtension.SourceGenerator" Version="1.0.0-beta1">
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

    [HttpApi]
    public async ValueTask<DataEntity?> Get([FromRoute] string id)
    {
        return await dataService.QueryDataAsync(id).ConfigureAwait(false);
    }

    [HttpApi]
    public async ValueTask<CrudCreateOutput> Create([FromBody] CrudCreateInput input)
    {
        var entity = mapper.Map<DataEntity>(input);
        entity.Id = Guid.NewGuid().ToString();
        entity.CreatedAt = DateTime.Now;

        await dataService.CreateDataAsync(entity).ConfigureAwait(false);

        logger.LogInformation("Data created. id=[{Id}]", entity.Id);

        return new CrudCreateOutput { Id = entity.Id };
    }

    [HttpApi]
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

            var output = await function.Get(p0);
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

            var output = await function.Create(p0);
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

            await function.Delete(p0);

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
