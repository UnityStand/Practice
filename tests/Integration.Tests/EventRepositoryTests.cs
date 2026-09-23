using Events.Domain.Entities;
using Events.Infrastructure.Persistence;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class EventRepositoryTests(PostgresContainerFixture fixture)
    : DatabaseTestBase<EventDbContext>(fixture, "events_db_test")
{
    protected override EventDbContext CreateContext(DbContextOptions<EventDbContext> options) => new(options);

    private EventRepository Repository => new(Context);

    private static Event CreateTestEvent(string title = "Test Event", int totalSeats = 10, DateTime? startAt = null, DateTime? endAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        return Event.Create(title, "description", start, endAt ?? start.AddHours(2), totalSeats);
    }

    [Fact]
    public async Task Migration_CreatesOnlyEventsTables()
    {
        var tables = await PublicTablesAsync();

        Assert.Equal(["Events", "ProcessedBookings", "__EFMigrationsHistory"], tables);
    }

    [Fact]
    public async Task GetEventByIdAsync_ReturnsEvent_WhenExists()
    {
        var ev = CreateTestEvent();
        await Repository.AddAsync(ev);

        var result = await new EventRepository(NewContext()).GetEventByIdAsync(ev.Id);

        Assert.NotNull(result);
        Assert.Equal(ev.Title, result.Title);
    }

    [Fact]
    public async Task GetEventByIdAsync_ReturnsNull_WhenNotExists()
    {
        Assert.Null(await Repository.GetEventByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var ev = CreateTestEvent(title: "Original");
        await Repository.AddAsync(ev);

        ev.UpdateInfo("Updated", "new description", ev.StartAt, ev.EndAt);
        await Repository.UpdateAsync(ev);

        var reloaded = await NewContext().Events.SingleAsync(e => e.Id == ev.Id);
        Assert.Equal("Updated", reloaded.Title);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEvent()
    {
        var ev = CreateTestEvent();
        await Repository.AddAsync(ev);

        await Repository.RemoveAsync(ev);

        Assert.Null(await new EventRepository(NewContext()).GetEventByIdAsync(ev.Id));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitle_IgnoringCase()
    {
        await Repository.AddAsync(CreateTestEvent(title: "Concert A"));
        await Repository.AddAsync(CreateTestEvent(title: "Concert B"));
        await Repository.AddAsync(CreateTestEvent(title: "Exhibition"));

        var (items, total) = await Repository.GetPagedAsync("concert", null, null, 1, 10);

        Assert.Equal(2, total);
        Assert.All(items, e => Assert.Contains("Concert", e.Title));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateRange()
    {
        var early = CreateTestEvent(startAt: DateTime.UtcNow.AddDays(1));
        var late = CreateTestEvent(startAt: DateTime.UtcNow.AddDays(10));
        await Repository.AddAsync(early);
        await Repository.AddAsync(late);

        var (items, total) = await Repository.GetPagedAsync(null, DateTime.UtcNow.AddDays(5), null, 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(late.Id, items.Single().Id);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesPagination()
    {
        for (var i = 0; i < 5; i++)
            await Repository.AddAsync(CreateTestEvent(title: $"Event {i}", startAt: DateTime.UtcNow.AddDays(i + 1)));

        var (page1, total) = await Repository.GetPagedAsync(null, null, null, 1, 2);
        var (page2, _) = await Repository.GetPagedAsync(null, null, null, 2, 2);

        Assert.Equal(5, total);
        Assert.Equal(2, page1.Count);
        Assert.DoesNotContain(page1, e1 => page2.Any(e2 => e2.Id == e1.Id));
    }
}
