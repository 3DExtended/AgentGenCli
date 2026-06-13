using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Handlers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Queries;

namespace AgentGenCli.Cqrs.EfCore.Tests;

public class CreateQueryHandlerTests : EfCoreTestBase
{
    [Fact]
    public async Task RunQueryAsync_CreatesEntity()
    {
        var model = new TestModel { Name = "sample" };
        var handler = new TestModelCreateQueryHandler(Mapper, ContextFactory);
        var result = await handler.RunQueryAsync(
            new TestModelCreateQuery { ModelToCreate = model },
            CancellationToken.None
        );

        Assert.True(result.IsSome);
        Assert.Single(Context.Entities);
        Assert.Equal("sample", Context.Entities.First().Name);
        Assert.Equal(Context.Entities.First().Id, result.Get().Value);
    }
}
