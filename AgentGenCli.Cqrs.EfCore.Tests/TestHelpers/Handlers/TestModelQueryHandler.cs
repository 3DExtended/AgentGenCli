using AgentGenCli.Cqrs.EfCore;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;

public class TestModelQueryHandler
    : SingleModelQueryHandlerBase<
        TestModelQuery,
        TestModel,
        TestModelId,
        Guid,
        TestDbContext,
        TestEntity
    >
{
    public TestModelQueryHandler(IMapper mapper, IDbContextFactory<TestDbContext> contextFactory)
        : base(mapper, contextFactory)
    {
    }
}
