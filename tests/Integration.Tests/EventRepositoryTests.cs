using ASP.NET_Core_Web_API.Models;
using Integration.Tests.Fixtures;

namespace Integration.Tests;

[Collection("Database")]
public class EventRepositoryTests : RepositoryTestBase
{
    public EventRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
    }

    private static Event CreateTestEvent(string title = "Test Event", int totalSeats = 10, DateTime? startAt = null, DateTime? endAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        var end = endAt ?? start.AddHours(2);
        return Event.Create(title, "description", start, end, totalSeats);
    }

    [Fact]
    public async Task GetEventByIdAsync_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        await EventRepository.AddAsync(testEvent);

        // Act
        var result = await EventRepository.GetEventByIdAsync(testEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testEvent.Id, result!.Id);
        Assert.Equal(testEvent.Title, result.Title);
    }

    [Fact]
    public async Task GetEventByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await EventRepository.GetEventByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsEvent()
    {
        // Arrange
        var testEvent = CreateTestEvent();

        // Act
        await EventRepository.AddAsync(testEvent);

        // Assert
        var saved = await Context.Events.FindAsync(testEvent.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        // Arrange
        var testEvent = CreateTestEvent(title: "Original");
        await EventRepository.AddAsync(testEvent);

        // Act
        testEvent.UpdateInfo("Updated", "new description", testEvent.StartAt, testEvent.EndAt);
        await EventRepository.UpdateAsync(testEvent);

        // Assert
        var reloaded = await EventRepository.GetEventByIdAsync(testEvent.Id);
        Assert.Equal("Updated", reloaded!.Title);
        Assert.Equal("new description", reloaded.Description);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEvent()
    {
        // Arrange
        var testEvent = CreateTestEvent();
        await EventRepository.AddAsync(testEvent);

        // Act
        await EventRepository.RemoveAsync(testEvent);

        // Assert
        var result = await EventRepository.GetEventByIdAsync(testEvent.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitle()
    {
        // Arrange
        await EventRepository.AddAsync(CreateTestEvent(title: "Concert A"));
        await EventRepository.AddAsync(CreateTestEvent(title: "Concert B"));
        await EventRepository.AddAsync(CreateTestEvent(title: "Exhibition"));

        // Act
        var (items, total) = await EventRepository.GetPagedAsync("concert", null, null, 1, 10);

        // Assert
        Assert.Equal(2, total);
        Assert.All(items, e => Assert.Contains("Concert", e.Title));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByFromDate()
    {
        // Arrange
        var early = CreateTestEvent(startAt: DateTime.UtcNow.AddDays(1));
        var late = CreateTestEvent(startAt: DateTime.UtcNow.AddDays(10));
        await EventRepository.AddAsync(early);
        await EventRepository.AddAsync(late);

        // Act
        var (items, total) = await EventRepository.GetPagedAsync(null, DateTime.UtcNow.AddDays(5), null, 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal(late.Id, items.Single().Id);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByToDate()
    {
        // Arrange
        var early = CreateTestEvent(startAt: DateTime.UtcNow.AddDays(1), endAt: DateTime.UtcNow.AddDays(1).AddHours(2));
        var late = CreateTestEvent(startAt: DateTime.UtcNow.AddDays(10), endAt: DateTime.UtcNow.AddDays(10).AddHours(2));
        await EventRepository.AddAsync(early);
        await EventRepository.AddAsync(late);

        // Act
        var (items, total) = await EventRepository.GetPagedAsync(null, null, DateTime.UtcNow.AddDays(5), 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal(early.Id, items.Single().Id);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesPagination()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await EventRepository.AddAsync(CreateTestEvent(title: $"Event {i}", startAt: DateTime.UtcNow.AddDays(i + 1)));
        }

        // Act
        var (page1, total1) = await EventRepository.GetPagedAsync(null, null, null, 1, 2);
        var (page2, total2) = await EventRepository.GetPagedAsync(null, null, null, 2, 2);

        // Assert
        Assert.Equal(5, total1);
        Assert.Equal(5, total2);
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.DoesNotContain(page1, e1 => page2.Any(e2 => e2.Id == e1.Id));
    }
}
