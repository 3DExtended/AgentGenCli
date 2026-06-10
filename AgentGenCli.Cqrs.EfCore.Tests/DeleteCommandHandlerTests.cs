using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;

namespace AgentGenCli.Cqrs.EfCore.Tests;

public class DeleteCommandHandlerTests : EfCoreTestBase
{
    [Fact]
    public async Task RunQueryAsync_DeletesExistingEntity()
    {
        var id = Guid.NewGuid();
        Context.Entities.Add(new TestEntity { Id = id, Name = "delete-me" });
        await Context.SaveChangesAsync();

        var handler = new TestModelDeleteCommandHandler(ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelDeleteCommand { Id = TestModelId.From(id) },
            CancellationToken.None
        );

        Assert.True(result.IsSome);
        Assert.Empty(Context.Entities);
    }

    [Fact]
    public async Task RunQueryAsync_ReturnsNoneWhenMissing()
    {
        var handler = new TestModelDeleteCommandHandler(ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelDeleteCommand { Id = TestModelId.From(Guid.NewGuid()) },
            CancellationToken.None
        );

        Assert.True(result.IsNone);
    }
}
