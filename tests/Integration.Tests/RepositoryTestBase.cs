using EventApi.Infrastructure.Persistence;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

[Collection("Database")]
public abstract class RepositoryTestBase : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;

    internal AppDbContext Context { get; private set; } = null!;
    internal EventRepository EventRepository { get; private set; } = null!;
    internal BookingRepository BookingRepository { get; private set; } = null!;

    protected RepositoryTestBase(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        Context = new AppDbContext(options);
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.MigrateAsync();

        EventRepository = new EventRepository(Context);
        BookingRepository = new BookingRepository(Context);
    }

    public Task DisposeAsync() => Context.DisposeAsync().AsTask();
}
