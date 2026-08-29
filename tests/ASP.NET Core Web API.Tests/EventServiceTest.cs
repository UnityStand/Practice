using System.ComponentModel.DataAnnotations;
using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASP.NET_Core_Web_API.Tests;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    private IEventService CreateEventService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEventService>();

    private static async Task<Event> CreateTestEvent(
        IEventService service,
        string title = "Test Event",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 10)
    {
        return await service.CreateEvent(
            title,
            null,
            startAt ?? DateTime.UtcNow,
            endAt ?? DateTime.UtcNow.AddHours(2),
            totalSeats);
    }

    private static async Task<List<Event>> SampleEvents(IEventService service) =>
    [
        await CreateTestEvent(service, title: "Null Meeting", startAt: new DateTime(2026, 1, 10), endAt: new DateTime(2026, 1, 10, 11, 0, 0)),
        await CreateTestEvent(service, title: "Conference", startAt: new DateTime(2026, 2, 1), endAt: new DateTime(2026, 2, 3)),
        await CreateTestEvent(service, title: "Daily meeting", startAt: new DateTime(2026, 3, 5), endAt: new DateTime(2026, 3, 5, 9, 30, 0)),
        await CreateTestEvent(service, title: "Daily StandUp", startAt: new DateTime(2026, 3, 6), endAt: new DateTime(2026, 3, 6, 9, 15, 0)),
        await CreateTestEvent(service, title: "Evryday routine", startAt: new DateTime(2026, 4, 1), endAt: new DateTime(2026, 4, 1, 8, 0, 0)),
        await CreateTestEvent(service, title: "Parents mEetInG", startAt: new DateTime(2026, 5, 15), endAt: new DateTime(2026, 5, 15, 18, 0, 0)),
        await CreateTestEvent(service, title: "meeting", startAt: new DateTime(2026, 6, 20), endAt: new DateTime(2026, 6, 20, 10, 0, 0))
    ];

    [Fact]
    public async Task CreateEvent_AssignsUniqueIds()
    {
        var service = CreateEventService();
        var first = await CreateTestEvent(service, title: "First Event");
        var second = await CreateTestEvent(service, title: "Second Event");

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task CreateEvent_Throws_WhenTotalSeatsIsNotPositive()
    {
        var service = CreateEventService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateEvent("Invalid Event", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0));
    }

    [Fact]
    public async Task CreateEvent_SetsAvailableSeatsEqualToTotalSeats()
    {
        var service = CreateEventService();
        var created = await CreateTestEvent(service, totalSeats: 5);

        Assert.Equal(5, created.TotalSeats);
        Assert.Equal(5, created.AvailableSeats);
    }

    [Fact]
    public async Task GetEventById_ReturnException_WhenNotFound()
    {
        var service = CreateEventService();
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetEventById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetEventById_ReturnEvent()
    {
        var service = CreateEventService();
        var events = await SampleEvents(service);

        var result = await service.GetEventById(events[0].Id);

        Assert.Equal(events[0].Id, result.Id);
        Assert.Equal("Null Meeting", result.Title);
    }

    [Fact]
    public async Task GetEvents_ReturnAllEvents_WhenEmptyFilters()
    {
        var service = CreateEventService();
        await SampleEvents(service);

        var result = await service.GetEvents(null, null, null);
        Assert.Equal(7, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FiltersByTitle_IgnoreCase()
    {
        var service = CreateEventService();
        await SampleEvents(service);

        var result = await service.GetEvents("meeting", null, null);

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FiltersByDateRange()
    {
        var service = CreateEventService();
        await SampleEvents(service);

        var result = await service.GetEvents(null, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_FiltersByTitleAndDateRange_IgnoreCase()
    {
        var service = CreateEventService();
        await SampleEvents(service);

        var result = await service.GetEvents("MEETING", new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task UpdateEvent_UpdatesExistingEvent()
    {
        var service = CreateEventService();
        var created = await CreateTestEvent(service, title: "Original Title");

        var updated = await service.UpdateEvent(
            created.Id,
            "Updated Title",
            "Updated description",
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 2));

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(new DateTime(2026, 7, 1), updated.StartAt);
        Assert.Equal(new DateTime(2026, 7, 2), updated.EndAt);
    }

    [Fact]
    public async Task UpdateEvent_Throws_WhenNotFound()
    {
        var service = CreateEventService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateEvent(
            Guid.NewGuid(),
            "Doesn't matter",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task DeleteEvent_RemovesExistingEvent()
    {
        var service = CreateEventService();
        var created = await CreateTestEvent(service);

        var result = await service.DeleteEvent(created.Id);

        Assert.True(result);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetEventById(created.Id));
    }

    [Fact]
    public async Task DeleteEvent_Throws_WhenNotFound()
    {
        var service = CreateEventService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteEvent(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteEvent_Throws_WhenEventHasBookings()
    {
        var service = CreateEventService();
        var created = await CreateTestEvent(service);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Bookings.Add(Booking.Create(created.Id, BookingStatus.Pending, DateTime.UtcNow));
            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<EventHasBookingsException>(() => service.DeleteEvent(created.Id));
    }

    [Fact]
    public async Task GetEvents_ReturnsCorrectPage()
    {
        var service = CreateEventService();
        await SampleEvents(service);

        var result = await service.GetEvents(null, null, null, page: 2, pageSize: 3);

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("Daily StandUp", result.Items[0].Title);
        Assert.Equal("Parents mEetInG", result.Items[2].Title);
    }

    [Fact]
    public async Task GetEvents_ReturnsPartialLastPage()
    {
        var service = CreateEventService();
        await SampleEvents(service);

        var result = await service.GetEvents(null, null, null, page: 3, pageSize: 3);

        Assert.Equal(7, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("meeting", result.Items[0].Title);
    }
}
