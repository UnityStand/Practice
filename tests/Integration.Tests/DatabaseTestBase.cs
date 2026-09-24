using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Integration.Tests;

// Один контейнер PostgreSQL на все тесты, но у каждого сервиса — своя база (как в docker-compose).
// Перед каждым тестом база пересоздаётся и к ней применяются миграции сервиса.
[Collection("Database")]
public abstract class DatabaseTestBase<TContext>(PostgresContainerFixture fixture, string database) : IAsyncLifetime
    where TContext : DbContext
{
    private DbContextOptions<TContext> _options = null!;

    protected TContext Context { get; private set; } = null!;

    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    // Отдельный контекст без кеша change tracker — чтобы читать то, что реально лежит в базе
    protected TContext NewContext() => CreateContext(_options);

    public async Task InitializeAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = database }.ConnectionString;
        _options = new DbContextOptionsBuilder<TContext>().UseNpgsql(connectionString).Options;

        Context = CreateContext(_options);
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Context.DisposeAsync().AsTask();

    protected Task<List<string>> PublicTablesAsync() =>
        Context.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name")
            .ToListAsync();
}
