using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using Microsoft.EntityFrameworkCore;

namespace AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;

public sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>
{
    private readonly DbContextOptions<TestDbContext> _options;

    public TestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared")
            .Options;
    }

    public TestDbContext CreateDbContext() => new(_options);

    public Task<TestDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
