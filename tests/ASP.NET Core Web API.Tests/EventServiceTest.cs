using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;

namespace ASP.NET_Core_Web_API.Tests;

public class EventServiceTests
{
    private static Event CreateEvent(
        string title = "Test Event",
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        return new Event
        {
            Title = title,
            StartAt = startAt ?? DateTime.UtcNow,
            EndAt = endAt ?? DateTime.UtcNow.AddHours(2)
        };
    }

    [Fact]
    public void CreateEvent_AssignsUniqueId()
    {
        // Arrange
        var service = new EventService();

        // Act
        var first = service.CreateEvent(CreateEvent(title: "First Event"));
        var second = service.CreateEvent(CreateEvent(title: "Second Event"));

        // Assert
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void GetEventById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var service = new EventService();

        // Act
        var result = service.GetEventById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetEventById_ReturnsEvent_WhenExists()
    {
        // Arrange
        var service = new EventService();
        var created = service.CreateEvent(CreateEvent(title: "Null Meeting"));

        // Act
        var result = service.GetEventById(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Null Meeting", result.Title);
    }

    [Fact]
    public void GetEvents_ReturnsAllCreatedEvents()
    {
        // Arrange
        var service = new EventService();
        service.CreateEvent(CreateEvent(title: "First"));
        service.CreateEvent(CreateEvent(title: "Second"));
        service.CreateEvent(CreateEvent(title: "Third"));

        // Act
        var result = service.GetEvents();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void UpdateEvent_UpdatesExistingEvent()
    {
        // Arrange
        var service = new EventService();
        var created = service.CreateEvent(CreateEvent(title: "Original Title"));

        // Act
        var updated = service.UpdateEvent(new Event
        {
            Id = created.Id,
            Title = "Updated Title",
            Description = "Updated description",
            StartAt = new DateTime(2026, 7, 1),
            EndAt = new DateTime(2026, 7, 2)
        });

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(new DateTime(2026, 7, 1), updated.StartAt);
        Assert.Equal(new DateTime(2026, 7, 2), updated.EndAt);
    }

    [Fact]
    public void UpdateEvent_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var service = new EventService();

        // Act
        var result = service.UpdateEvent(new Event
        {
            Id = Guid.NewGuid(),
            Title = "Doesn't matter",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(1)
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeleteEvent_RemovesExistingEvent()
    {
        // Arrange
        var service = new EventService();
        var created = service.CreateEvent(CreateEvent());

        // Act
        var result = service.DeleteEvent(created.Id);

        // Assert
        Assert.True(result);
        Assert.Null(service.GetEventById(created.Id));
    }

    [Fact]
    public void DeleteEvent_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var service = new EventService();

        // Act
        var result = service.DeleteEvent(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }
}