using AgentGenCli.Cqrs.EfCore;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;

public class TestModelListQueryHandler
    : ListOfModelQueryHandlerBase<
        TestModelListQuery,
        TestModel,
        TestModelId,
        Guid,
        TestDbContext,
        TestEntity
    >
{
    public TestModelListQueryHandler(IMapper mapper, IDbContextFactory<TestDbContext> contextFactory)
        : base(mapper, contextFactory)
    {
    }
}
