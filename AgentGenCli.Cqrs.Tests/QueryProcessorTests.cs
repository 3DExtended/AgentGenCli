using AgentGenCli.Cqrs;
using AgentGenCli.Cqrs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AgentGenCli.Cqrs.Tests;

public class QueryProcessorTests
{
    [Fact]
    public async Task RunQueryAsync_dispatches_to_registered_handler()
    {
        var services = new ServiceCollection();
        services.AddCqrs(o => o.WithQueryHandlersFrom(typeof(PingQueryHandler).Assembly));

        await using var provider = services.BuildServiceProvider();
        var processor = provider.GetRequiredService<IQueryProcessor>();

        var result = await processor.RunQueryAsync(new PingQuery(), CancellationToken.None);

        Assert.True(result.IsSome);
        Assert.Equal("pong", result.Get());
    }

    private sealed class PingQuery : IQuery<string, PingQuery>;

    private sealed class PingQueryHandler : IQueryHandler<PingQuery, string>
    {
        public Task<Option<string>> RunQueryAsync(PingQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<Option<string>>("pong");
    }
}
