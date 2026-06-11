namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;

public class TestEntityWithNav : TestEntity
{
    public TestEntity? Parent { get; set; }
}
