using AgentGenCli.Cqrs;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;
using Microsoft.EntityFrameworkCore;

namespace AgentGenCli.Cqrs.EfCore.Tests;

public class UpdateCommandHandlerTests : EfCoreTestBase
{
    [Fact]
    public async Task RunQueryAsync_UpdatesExistingEntity()
    {
        var id = Guid.NewGuid();
        Context.Entities.Add(new TestEntity { Id = id, Name = "original" });
        await Context.SaveChangesAsync();

        var handler = new TestModelUpdateCommandHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelUpdateCommand
            {
                UpdatedModel = new TestModel { Id = TestModelId.From(id), Name = "updated" },
            },
            CancellationToken.None
        );

        Assert.True(result.IsSome);
        Assert.Equal(Unit.Value, result.Get());

        await using var verifyContext = ContextFactory.CreateDbContext();
        var entity = await verifyContext.Entities.SingleAsync();
        Assert.Equal("updated", entity.Name);
    }

    [Fact]
    public async Task RunQueryAsync_ReturnsNoneWhenMissing()
    {
        var handler = new TestModelUpdateCommandHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelUpdateCommand
            {
                UpdatedModel = new TestModel
                {
                    Id = TestModelId.From(Guid.NewGuid()),
                    Name = "updated",
                },
            },
            CancellationToken.None
        );

        Assert.True(result.IsNone);
    }
}
