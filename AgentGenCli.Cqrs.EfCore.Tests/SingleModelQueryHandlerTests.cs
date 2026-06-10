using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;

namespace AgentGenCli.Cqrs.EfCore.Tests;

public class SingleModelQueryHandlerTests : EfCoreTestBase
{
    [Fact]
    public async Task RunQueryAsync_ReturnsModelWhenFound()
    {
        var id = Guid.NewGuid();
        Context.Entities.Add(new TestEntity { Id = id, Name = "found" });
        await Context.SaveChangesAsync();

        var handler = new TestModelQueryHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelQuery { ModelId = TestModelId.From(id) },
            CancellationToken.None
        );

        Assert.True(result.IsSome);
        Assert.Equal("found", result.Get().Name);
    }

    [Fact]
    public async Task RunQueryAsync_ReturnsNoneWhenMissing()
    {
        var handler = new TestModelQueryHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelQuery { ModelId = TestModelId.From(Guid.NewGuid()) },
            CancellationToken.None
        );

        Assert.True(result.IsNone);
    }
}
