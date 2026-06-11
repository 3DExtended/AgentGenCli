using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using Mapster;
using MapsterMapper;

namespace AgentGenCli.Cqrs.EfCore.Tests.Mapping;

public class MapsterMappingAssertTests
{
    [Fact]
    public void AssertRoundTrip_TestEntityToTestModel_Succeeds()
    {
        var mapper = MapsterTestMapperFactory.Create<TestMapsterConfig>();

        MapsterMappingAssert.AssertRoundTrip<TestEntity, TestModel>(mapper);
    }

    [Fact]
    public void AssertRoundTrip_TestModelToTestEntity_Succeeds()
    {
        var mapper = MapsterTestMapperFactory.Create<TestMapsterConfig>();

        MapsterMappingAssert.AssertRoundTrip<TestModel, TestEntity>(mapper);
    }

    [Fact]
    public void AssertRoundTrip_WhenPropertyIsNotMapped_Throws()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<TestEntity, TestModel>()
            .Map(dest => dest.Id, src => TestModelId.From(src.Id))
            .Map(dest => dest.Name, _ => "not-round-tripped");
        config.NewConfig<TestModel, TestEntity>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name);

        var mapper = new Mapper(config);

        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            MapsterMappingAssert.AssertRoundTrip<TestEntity, TestModel>(mapper)
        );

        Assert.Contains(nameof(TestEntity.Name), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssertRoundTrip_WhenPropertyIsExcluded_SkipsComparison()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<TestEntity, TestModel>()
            .Map(dest => dest.Id, src => TestModelId.From(src.Id));
        config.NewConfig<TestModel, TestEntity>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name);

        var mapper = new Mapper(config);
        var excluded = new[] { (typeof(TestEntity), nameof(TestEntity.Name)) };

        MapsterMappingAssert.AssertRoundTrip<TestEntity, TestModel>(mapper, excluded);
    }

    [Fact]
    public void AssertRoundTrip_WhenReferencePropertyIsNotExcluded_ThrowsBeforeMapping()
    {
        var mapper = MapsterTestMapperFactory.Create<TestMapsterConfig>();

        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            MapsterMappingAssert.AssertRoundTrip<TestEntityWithNav, TestModel>(mapper)
        );

        Assert.Contains("ExcludedProperties", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestEntityWithNav.Parent), exception.Message, StringComparison.Ordinal);
    }
}
