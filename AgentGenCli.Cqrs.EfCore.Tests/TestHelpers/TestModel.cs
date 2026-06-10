using AgentGenCli.Cqrs.EfCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;

public class TestModel : ModelBase<TestModelId, Guid>
{
    public string Name { get; set; } = string.Empty;
}
