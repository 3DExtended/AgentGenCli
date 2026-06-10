using AgentGenCli.Cqrs.EfCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;

public record TestModelId : Identifier<Guid, TestModelId>;
