using System.ComponentModel.DataAnnotations;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;

namespace ASP.NET_Core_Web_API.Tests;

public class EventServiceTests
{
    private static Event CreateTestEvent(
        IEventService service,
        string title = "Test Event",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 10)
    {
        return service.CreateEvent(
            title,
            null,
            startAt ?? DateTime.UtcNow,
            endAt ?? DateTime.UtcNow.AddHours(2),
            totalSeats);
    }

    private static List<Event> SampleEvents(IEventService service) =>
    [
        CreateTestEvent(service, title: "Null Meeting", startAt: new DateTime(2026, 1, 10), endAt: new DateTime(2026, 1, 10, 11, 0, 0)),
        CreateTestEvent(service, title: "Conference", startAt: new DateTime(2026, 2, 1), endAt: new DateTime(2026, 2, 3)),
        CreateTestEvent(service, title: "Daily meeting", startAt: new DateTime(2026, 3, 5), endAt: new DateTime(2026, 3, 5, 9, 30, 0)),
        CreateTestEvent(service, title: "Daily StandUp", startAt: new DateTime(2026, 3, 6), endAt: new DateTime(2026, 3, 6, 9, 15, 0)),
        CreateTestEvent(service, title: "Evryday routine", startAt: new DateTime(2026, 4, 1), endAt: new DateTime(2026, 4, 1, 8, 0, 0)),
        CreateTestEvent(service, title: "Parents mEetInG", startAt: new DateTime(2026, 5, 15), endAt: new DateTime(2026, 5, 15, 18, 0, 0)),
        CreateTestEvent(service, title: "meeting", startAt: new DateTime(2026, 6, 20), endAt: new DateTime(2026, 6, 20, 10, 0, 0))
    ];

    [Fact]
    public void CreateEvent_AssignsIncrementingIds()
    {
        var service = new EventService();
        var first = CreateTestEvent(service, title: "First Event");
        var second = CreateTestEvent(service, title: "Second Event");

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
    }

    [Fact]
    public void CreateEvent_Throws_WhenTotalSeatsIsNotPositive()
    {
        var service = new EventService();

        Assert.Throws<ValidationException>(() =>
            service.CreateEvent("Invalid Event", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0));
    }

    [Fact]
    public void CreateEvent_SetsAvailableSeatsEqualToTotalSeats()
    {
        var service = new EventService();
        var created = CreateTestEvent(service, totalSeats: 5);

        Assert.Equal(5, created.TotalSeats);
        Assert.Equal(5, created.AvailableSeats);
    }

    [Fact]
    public void GetEventById_ReturnException_WhenNotFound()
    {
        var service = new EventService();
        Assert.Throws<NotFoundException>(() => service.GetEventById(9999));
    }

    [Fact]
    public void GetEventById_ReturnEvent()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEventById(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Null Meeting", result.Title);
    }

    [Fact]
    public void GetEvents_ReturnAllEvents_WhenEmptyFilters()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEvents(null, null, null);
        Assert.Equal(7, result.TotalCount);
    }

    [Fact]
    public void GetEvents_FiltersByTitle_IgnoreCase()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEvents("meeting", null, null);

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public void GetEvents_FiltersByDateRange()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEvents(null, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public void GetEvents_FiltersByTitleAndDateRange_IgnoreCase()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEvents("MEETING", new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public void UpdateEvent_UpdatesExistingEvent()
    {
        var service = new EventService();
        var created = CreateTestEvent(service, title: "Original Title");

        var updated = service.UpdateEvent(new Event
        {
            Id = created.Id,
            Title = "Updated Title",
            Description = "Updated description",
            StartAt = new DateTime(2026, 7, 1),
            EndAt = new DateTime(2026, 7, 2)
        });

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(new DateTime(2026, 7, 1), updated.StartAt);
        Assert.Equal(new DateTime(2026, 7, 2), updated.EndAt);
    }

    [Fact]
    public void UpdateEvent_Throws_WhenNotFound()
    {
        var service = new EventService();

        Assert.Throws<NotFoundException>(() => service.UpdateEvent(new Event
        {
            Id = 9999,
            Title = "Doesn't matter",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(1)
        }));
    }

    [Fact]
    public void DeleteEvent_RemovesExistingEvent()
    {
        var service = new EventService();
        var created = CreateTestEvent(service);

        var result = service.DeleteEvent(created.Id);

        Assert.True(result);
        Assert.Throws<NotFoundException>(() => service.GetEventById(created.Id));
    }

    [Fact]
    public void DeleteEvent_Throws_WhenNotFound()
    {
        var service = new EventService();

        Assert.Throws<NotFoundException>(() => service.DeleteEvent(9999));
    }

    [Fact]
    public void GetEvents_ReturnsCorrectPage()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEvents(null, null, null, page: 2, pageSize: 3);

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("Daily StandUp", result.Items[0].Title);
        Assert.Equal("Parents mEetInG", result.Items[2].Title);
    }

    [Fact]
    public void GetEvents_ReturnsPartialLastPage()
    {
        var service = new EventService();
        SampleEvents(service);

        var result = service.GetEvents(null, null, null, page: 3, pageSize: 3);

        Assert.Equal(7, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("meeting", result.Items[0].Title);
    }
}
