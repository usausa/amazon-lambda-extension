namespace AmazonLambdaExtension.Example.Functions;

using AmazonLambdaExtension.Annotations;

using AmazonLambdaExtension.Example.Models;
using AmazonLambdaExtension.Example.Parameters;
using AmazonLambdaExtension.Example.Services;

using AutoMapper;

using Microsoft.Extensions.Logging;

[Lambda]
[ServiceResolver(typeof(ServiceResolver))]
[Filter(typeof(ApiFilter))]
public sealed class CrudFunction
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
