using AgentGenCli.Cqrs.EfCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;

public class TestEntity : IIdentifiableEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
