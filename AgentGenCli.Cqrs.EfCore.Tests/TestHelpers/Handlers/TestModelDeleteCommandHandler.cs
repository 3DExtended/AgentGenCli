using AgentGenCli.Cqrs.EfCore;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;
using Microsoft.EntityFrameworkCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;

public class TestModelDeleteCommandHandler
    : DeleteCommandHandlerBase<
        TestModelDeleteCommand,
        TestModel,
        TestModelId,
        Guid,
        TestDbContext,
        TestEntity
    >
{
    public TestModelDeleteCommandHandler(IDbContextFactory<TestDbContext> contextFactory)
        : base(contextFactory)
    {
    }
}
