using System.Collections.Concurrent;
using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASP.NET_Core_Web_API.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    private IEventService CreateEventService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEventService>();

    private IBookingService CreateBookingService() =>
        _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IBookingService>();

    private async Task<Event> CreateTestEvent(string title = "Test Event", int totalSeats = 10)
    {
        return await CreateEventService().CreateEvent(title, null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), totalSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsPendingBooking_WhenEventExists()
    {
        var ev = await CreateTestEvent();

        var booking = await CreateBookingService().CreateBookingAsync(ev.Id);

        Assert.Equal(ev.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotEqual(Guid.Empty, booking.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_AssignsUniqueIds_ForMultipleBookings()
    {
        var ev = await CreateTestEvent();

        var first = await CreateBookingService().CreateBookingAsync(ev.Id);
        var second = await CreateBookingService().CreateBookingAsync(ev.Id);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsBooking_WhenExists()
    {
        var ev = await CreateTestEvent();
        var created = await CreateBookingService().CreateBookingAsync(ev.Id);

        var result = await CreateBookingService().GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReflectsStatusChange_AfterUpdate()
    {
        var ev = await CreateTestEvent();
        var created = await CreateBookingService().CreateBookingAsync(ev.Id);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var booking = await context.Bookings.FindAsync(created.Id);
            booking!.Confirm();
            await context.SaveChangesAsync();
        }

        var result = await CreateBookingService().GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenEventDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateBookingService().CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenEventWasDeleted()
    {
        var ev = await CreateTestEvent();
        await CreateEventService().DeleteEvent(ev.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateBookingService().CreateBookingAsync(ev.Id));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Throws_WhenBookingDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateBookingService().GetBookingByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        var ev = await CreateTestEvent(totalSeats: 5);

        await CreateBookingService().CreateBookingAsync(ev.Id);

        var result = await CreateEventService().GetEventById(ev.Id);
        Assert.Equal(4, result.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_AllowsBookingsUpToCapacity_AllSucceedWithUniqueIds()
    {
        var ev = await CreateTestEvent(totalSeats: 3);

        var first = await CreateBookingService().CreateBookingAsync(ev.Id);
        var second = await CreateBookingService().CreateBookingAsync(ev.Id);
        var third = await CreateBookingService().CreateBookingAsync(ev.Id);

        var ids = new[] { first.Id, second.Id, third.Id };
        Assert.Equal(3, ids.Distinct().Count());

        var result = await CreateEventService().GetEventById(ev.Id);
        Assert.Equal(0, result.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenSeatsExhausted()
    {
        var ev = await CreateTestEvent(totalSeats: 1);
        await CreateBookingService().CreateBookingAsync(ev.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => CreateBookingService().CreateBookingAsync(ev.Id));
    }

    [Fact]
    public async Task ReleaseSeats_AfterReject_RestoresAvailableSeats()
    {
        var ev = await CreateTestEvent(totalSeats: 1);
        var booking = await CreateBookingService().CreateBookingAsync(ev.Id);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trackedBooking = await context.Bookings.FindAsync(booking.Id);
            var trackedEvent = await context.Events.FindAsync(ev.Id);
            trackedBooking!.Reject();
            trackedEvent!.ReleaseSeats();
            await context.SaveChangesAsync();
        }

        var result = await CreateEventService().GetEventById(ev.Id);
        Assert.Equal(1, result.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_SucceedsAgain_AfterPreviousBookingReleasedSeat()
    {
        var ev = await CreateTestEvent(totalSeats: 1);
        var firstBooking = await CreateBookingService().CreateBookingAsync(ev.Id);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trackedBooking = await context.Bookings.FindAsync(firstBooking.Id);
            var trackedEvent = await context.Events.FindAsync(ev.Id);
            trackedBooking!.Reject();
            trackedEvent!.ReleaseSeats();
            await context.SaveChangesAsync();
        }

        var secondBooking = await CreateBookingService().CreateBookingAsync(ev.Id);

        Assert.NotEqual(firstBooking.Id, secondBooking.Id);

        var result = await CreateEventService().GetEventById(ev.Id);
        Assert.Equal(0, result.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UnderConcurrency_AllowsExactlyCapacityBookings()
    {
        var ev = await CreateTestEvent(totalSeats: 5);
        const int concurrentRequests = 20;
        var successCount = 0;
        var noSeatsCount = 0;

        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                await bookingService.CreateBookingAsync(ev.Id);
                Interlocked.Increment(ref successCount);
            }
            catch (NoAvailableSeatsException)
            {
                Interlocked.Increment(ref noSeatsCount);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(5, successCount);
        Assert.Equal(15, noSeatsCount);

        var result = await CreateEventService().GetEventById(ev.Id);
        Assert.Equal(0, result.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UnderConcurrency_AllBookingsHaveUniqueIds()
    {
        var ev = await CreateTestEvent(totalSeats: 10);
        const int concurrentRequests = 10;
        var bookings = new ConcurrentBag<Booking>();

        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var booking = await bookingService.CreateBookingAsync(ev.Id);
            bookings.Add(booking);
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentRequests, bookings.Count);
        Assert.Equal(concurrentRequests, bookings.Select(b => b.Id).Distinct().Count());
    }
}

public class BookingTests
{
    [Fact]
    public void Confirm_SetsStatusConfirmedAndProcessedAt()
    {
        var booking = Booking.Create(Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Reject_SetsStatusRejectedAndProcessedAt()
    {
        var booking = Booking.Create(Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }
}
