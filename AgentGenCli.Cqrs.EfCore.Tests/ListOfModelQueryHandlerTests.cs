using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;

namespace AgentGenCli.Cqrs.EfCore.Tests;

public class ListOfModelQueryHandlerTests : EfCoreTestBase
{
    [Fact]
    public async Task RunQueryAsync_WhenEmpty_ReturnsEmptyList()
    {
        var handler = new TestModelListQueryHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(new TestModelListQuery(), CancellationToken.None);

        Assert.True(result.IsSome);
        Assert.Empty(result.Get());
    }

    [Fact]
    public async Task RunQueryAsync_WhenSeeded_ReturnsAllItems()
    {
        var id = Guid.NewGuid();
        Context.Entities.Add(new TestEntity { Id = id, Name = "listed" });
        await Context.SaveChangesAsync();

        var handler = new TestModelListQueryHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(new TestModelListQuery(), CancellationToken.None);

        Assert.True(result.IsSome);
        var item = Assert.Single(result.Get());
        Assert.Equal(TestModelId.From(id), item.Id);
        Assert.Equal("listed", item.Name);
    }

    [Fact]
    public async Task RunQueryAsync_WhenPartialIdSet_ReturnsNone()
    {
        var id = Guid.NewGuid();
        Context.Entities.Add(new TestEntity { Id = id, Name = "listed" });
        await Context.SaveChangesAsync();

        var handler = new TestModelListQueryHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelListQuery
            {
                Ids = new[] { TestModelId.From(id), TestModelId.From(Guid.NewGuid()) },
            },
            CancellationToken.None
        );

        Assert.True(result.IsNone);
    }
}
