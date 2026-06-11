using Mapster;
using MapsterMapper;

namespace AgentGenCli.Cqrs.EfCore.Tests.Mapping;

public static class MapsterTestMapperFactory
{
    public static IMapper Create<TRegister>()
        where TRegister : IRegister, new()
    {
        var config = new TypeAdapterConfig();
        new TRegister().Register(config);
        return new Mapper(config);
    }
}
