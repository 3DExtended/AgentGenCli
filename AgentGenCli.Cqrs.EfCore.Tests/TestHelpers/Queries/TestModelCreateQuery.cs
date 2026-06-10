using AgentGenCli.Cqrs.EfCore;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;

public class TestModelCreateQuery
    : CreateQuery<TestModel, TestModelId, Guid, TestModelCreateQuery>;
