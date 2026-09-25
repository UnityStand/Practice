using System.ComponentModel.DataAnnotations;
using Events.Application.DTOs;
using Events.Application.Services;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Events.Infrastructure.Persistence;

namespace Events.Tests;

public class EventServiceTests : IDisposable
{
    private readonly TestServices _services = new();

    public void Dispose() => _services.Dispose();

    private IEventService Service() => _services.Create<IEventService>();

    private static Task<EventResponseDto> CreateTestEvent(
        IEventService service,
        string title = "Test Event",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 10) =>
        service.CreateEvent(title, null, startAt ?? DateTime.UtcNow, endAt ?? DateTime.UtcNow.AddHours(2), totalSeats);

    private static async Task<List<EventResponseDto>> SampleEvents(IEventService service) =>
    [
        await CreateTestEvent(service, title: "Null Meeting", startAt: new DateTime(2026, 1, 10), endAt: new DateTime(2026, 1, 10, 11, 0, 0)),
        await CreateTestEvent(service, title: "Conference", startAt: new DateTime(2026, 2, 1), endAt: new DateTime(2026, 2, 3)),
        await CreateTestEvent(service, title: "Daily meeting", startAt: new DateTime(2026, 3, 5), endAt: new DateTime(2026, 3, 5, 9, 30, 0)),
        await CreateTestEvent(service, title: "Daily StandUp", startAt: new DateTime(2026, 3, 6), endAt: new DateTime(2026, 3, 6, 9, 15, 0)),
        await CreateTestEvent(service, title: "Evryday routine", startAt: new DateTime(2026, 4, 1), endAt: new DateTime(2026, 4, 1, 8, 0, 0)),
        await CreateTestEvent(service, title: "Parents mEetInG", startAt: new DateTime(2026, 5, 15), endAt: new DateTime(2026, 5, 15, 18, 0, 0)),
        await CreateTestEvent(service, title: "meeting", startAt: new DateTime(2026, 6, 20), endAt: new DateTime(2026, 6, 20, 10, 0, 0))
    ];

    private async Task MarkBookingProcessed(Guid eventId)
    {
        var context = _services.Create<EventDbContext>();
        context.ProcessedBookings.Add(ProcessedBooking.Create(Guid.NewGuid(), eventId, DateTime.UtcNow));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateEvent_AssignsUniqueIds()
    {
        var service = Service();
        var first = await CreateTestEvent(service, title: "First Event");
        var second = await CreateTestEvent(service, title: "Second Event");

        Assert.NotEqual(Guid.Empty, first.EventId);
        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public async Task CreateEvent_Throws_WhenTotalSeatsIsNotPositive()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Service().CreateEvent("Invalid Event", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0));
    }

    [Fact]
    public async Task CreateEvent_SetsAvailableSeatsEqualToTotalSeats()
    {
        var created = await CreateTestEvent(Service(), totalSeats: 5);

        Assert.Equal(5, created.TotalSeats);
        Assert.Equal(5, created.AvailableSeats);
    }

    [Fact]
    public async Task GetEventById_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetEventById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetEventById_ReturnsEvent()
    {
        var events = await SampleEvents(Service());

        var result = await Service().GetEventById(events[0].EventId);

        Assert.Equal("Null Meeting", result.Title);
    }

    [Fact]
    public async Task GetEvents_ReturnsAllEvents_WhenFiltersEmpty()
    {
        await SampleEvents(Service());

        var result = await Service().GetEvents(null, null, null);

        Assert.Equal(7, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FiltersByTitle_IgnoringCase()
    {
        await SampleEvents(Service());

        var result = await Service().GetEvents("meeting", null, null);

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FiltersByDateRange()
    {
        await SampleEvents(Service());

        var result = await Service().GetEvents(null, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FiltersByTitleAndDateRange()
    {
        await SampleEvents(Service());

        var result = await Service().GetEvents("MEETING", new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_ReturnsRequestedPage()
    {
        await SampleEvents(Service());

        var result = await Service().GetEvents(null, null, null, page: 2, pageSize: 3);

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("Daily StandUp", result.Items[0].Title);
        Assert.Equal("Parents mEetInG", result.Items[2].Title);
    }

    [Fact]
    public async Task GetEvents_ReturnsPartialLastPage()
    {
        await SampleEvents(Service());

        var result = await Service().GetEvents(null, null, null, page: 3, pageSize: 3);

        Assert.Single(result.Items);
        Assert.Equal("meeting", result.Items[0].Title);
    }

    [Fact]
    public async Task UpdateEvent_UpdatesExistingEvent()
    {
        var created = await CreateTestEvent(Service(), title: "Original Title");

        var updated = await Service().UpdateEvent(
            created.EventId, "Updated Title", "Updated description", new DateTime(2026, 7, 1), new DateTime(2026, 7, 2));

        Assert.Equal(created.EventId, updated.EventId);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task UpdateEvent_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service().UpdateEvent(Guid.NewGuid(), "Title", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task DeleteEvent_RemovesEvent_WhenNoProcessedBookings()
    {
        var created = await CreateTestEvent(Service());

        Assert.True(await Service().DeleteEvent(created.EventId));
        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetEventById(created.EventId));
    }

    [Fact]
    public async Task DeleteEvent_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service().DeleteEvent(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteEvent_Throws_WhenEventHasProcessedBookings()
    {
        var created = await CreateTestEvent(Service());
        await MarkBookingProcessed(created.EventId);

        await Assert.ThrowsAsync<EventHasBookingsException>(() => Service().DeleteEvent(created.EventId));
    }

    [Fact]
    public async Task DeleteEvent_Succeeds_WhenProcessedBookingsBelongToAnotherEvent()
    {
        var target = await CreateTestEvent(Service(), title: "Target");
        var other = await CreateTestEvent(Service(), title: "Other");
        await MarkBookingProcessed(other.EventId);

        Assert.True(await Service().DeleteEvent(target.EventId));
    }
}
