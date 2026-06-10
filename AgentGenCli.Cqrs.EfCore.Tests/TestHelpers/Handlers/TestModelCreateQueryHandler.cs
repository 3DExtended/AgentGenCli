using AgentGenCli.Cqrs.EfCore;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;

public class TestModelCreateQueryHandler
    : CreateQueryHandlerBase<
        TestModelCreateQuery,
        TestModel,
        TestModelId,
        Guid,
        TestDbContext,
        TestEntity
    >
{
    public TestModelCreateQueryHandler(IMapper mapper, IDbContextFactory<TestDbContext> contextFactory)
        : base(mapper, contextFactory)
    {
    }
}
