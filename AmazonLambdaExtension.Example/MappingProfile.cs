namespace AmazonLambdaExtension.Example;

using AmazonLambdaExtension.Example.Models;
using AmazonLambdaExtension.Example.Parameters;

using AutoMapper;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CrudCreateInput, DataEntity>();
    }
}
