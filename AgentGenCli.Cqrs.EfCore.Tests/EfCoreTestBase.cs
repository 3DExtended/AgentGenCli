using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers;
using AgentGenCli.Cqrs.EfCore.Tests.TestHelpers.Context;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgentGenCli.Cqrs.EfCore.Tests;

public abstract class EfCoreTestBase : IDisposable
{
    private readonly string _runIdentifier = Guid.NewGuid().ToString();
    private bool _isDisposed;

    protected EfCoreTestBase()
    {
        var services = new ServiceCollection()
            .AddDbContextFactory<TestDbContext>(options =>
                options.UseSqlite($"Data Source=file:memdb{_runIdentifier}?mode=memory&cache=shared")
            );

        var serviceProvider = services.BuildServiceProvider();
        ContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>();

        var config = new TypeAdapterConfig();
        config.NewConfig<TestEntity, TestModel>()
            .Map(dest => dest.Id, src => TestHelpers.TestModelId.From(src.Id))
            .Map(dest => dest.Name, src => src.Name);
        config.NewConfig<TestModel, TestEntity>()
            .Map(dest => dest.Id, src => src.Id.Value == Guid.Empty ? Guid.NewGuid() : src.Id.Value)
            .Map(dest => dest.Name, src => src.Name);

        Mapper = new Mapper(config);

        Context = ContextFactory.CreateDbContext();
        Context.Database.OpenConnection();
        Context.Database.EnsureCreated();
    }

    protected IMapper Mapper { get; }

    protected IDbContextFactory<TestDbContext> ContextFactory { get; }

    protected TestDbContext Context { get; }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Context.Database.CloseConnection();
        Context.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
