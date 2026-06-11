using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using Mapster;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;

public class TestMapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TestEntity, TestModel>()
            .Map(dest => dest.Id, src => TestModelId.From(src.Id))
            .Map(dest => dest.Name, src => src.Name);

        config.NewConfig<TestModel, TestEntity>()
            .Map(dest => dest.Id, src => src.Id.Value == Guid.Empty ? Guid.NewGuid() : src.Id.Value)
            .Map(dest => dest.Name, src => src.Name);
    }
}
