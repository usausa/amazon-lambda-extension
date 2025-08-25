namespace AmazonLambdaExtension.Example;

using AutoMapper;

using AmazonLambdaExtension.Example.Models;
using AmazonLambdaExtension.Example.Parameters;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CrudCreateInput, DataEntity>();
    }
}
