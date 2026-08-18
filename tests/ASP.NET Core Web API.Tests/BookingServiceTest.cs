using System.Collections.Concurrent;
using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;

namespace ASP.NET_Core_Web_API.Tests;

public class BookingServiceTests
{
    private static (BookingService bookingService, IEventService eventService, IBookingStore bookingStore) CreateSut()
    {
        var eventService = new EventService(new InMemoryEventStore());
        var bookingStore = new InMemoryBookingStore();
        var bookingService = new BookingService(bookingStore, eventService);
        return (bookingService, eventService, bookingStore);
    }

    private static Event CreateTestEvent(IEventService eventService, string title = "Test Event", int totalSeats = 10)
    {
        return eventService.CreateEvent(title, null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), totalSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsPendingBooking_WhenEventExists()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService);

        var booking = await bookingService.CreateBookingAsync(ev.Id);

        Assert.Equal(ev.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotEqual(Guid.Empty, booking.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_AssignsUniqueIds_ForMultipleBookings()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService);

        var first = await bookingService.CreateBookingAsync(ev.Id);
        var second = await bookingService.CreateBookingAsync(ev.Id);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsBooking_WhenExists()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService);
        var created = await bookingService.CreateBookingAsync(ev.Id);

        var result = await bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReflectsStatusChange_AfterUpdate()
    {
        var (bookingService, eventService, bookingStore) = CreateSut();
        var ev = CreateTestEvent(eventService);
        var created = await bookingService.CreateBookingAsync(ev.Id);
        created.Status = BookingStatus.Confirmed;
        created.ProcessedAt = DateTime.UtcNow;
        bookingStore.UpdateBooking(created);

        var result = await bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenEventDoesNotExist()
    {
        var (bookingService, _, _) = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenEventWasDeleted()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService);
        eventService.DeleteEvent(ev.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.CreateBookingAsync(ev.Id));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Throws_WhenBookingDoesNotExist()
    {
        var (bookingService, _, _) = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(() => bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 5);

        await bookingService.CreateBookingAsync(ev.Id);

        Assert.Equal(4, eventService.GetEventById(ev.Id).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_AllowsBookingsUpToCapacity_AllSucceedWithUniqueIds()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 3);

        var first = await bookingService.CreateBookingAsync(ev.Id);
        var second = await bookingService.CreateBookingAsync(ev.Id);
        var third = await bookingService.CreateBookingAsync(ev.Id);

        var ids = new[] { first.Id, second.Id, third.Id };
        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(0, eventService.GetEventById(ev.Id).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenSeatsExhausted()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 1);
        await bookingService.CreateBookingAsync(ev.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => bookingService.CreateBookingAsync(ev.Id));
    }

    [Fact]
    public async Task ReleaseSeats_AfterReject_RestoresAvailableSeats()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 1);
        var booking = await bookingService.CreateBookingAsync(ev.Id);

        booking.Reject();
        eventService.GetEventById(ev.Id).ReleaseSeats();

        Assert.Equal(1, eventService.GetEventById(ev.Id).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_SucceedsAgain_AfterPreviousBookingReleasedSeat()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 1);
        var firstBooking = await bookingService.CreateBookingAsync(ev.Id);

        firstBooking.Reject();
        eventService.GetEventById(ev.Id).ReleaseSeats();

        var secondBooking = await bookingService.CreateBookingAsync(ev.Id);

        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(0, eventService.GetEventById(ev.Id).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UnderConcurrency_AllowsExactlyCapacityBookings()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 5);
        const int concurrentRequests = 20;
        var successCount = 0;
        var noSeatsCount = 0;

        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            try
            {
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
        Assert.Equal(0, eventService.GetEventById(ev.Id).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UnderConcurrency_AllBookingsHaveUniqueIds()
    {
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateTestEvent(eventService, totalSeats: 10);
        const int concurrentRequests = 10;
        var bookings = new ConcurrentBag<Booking>();

        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
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

public class InMemoryBookingStoreTests
{
    [Fact]
    public void GetBookingsPending_ExcludesConfirmedBookings()
    {
        var store = new InMemoryBookingStore();
        var pending = Booking.Create(Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
        var confirmed = Booking.Create(Guid.NewGuid(), BookingStatus.Confirmed, DateTime.UtcNow);
        store.AddBooking(pending);
        store.AddBooking(confirmed);

        var result = store.GetBookingsPending().ToList();

        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
    }

    [Fact]
    public void UpdateBooking_PersistsStatusChange()
    {
        var store = new InMemoryBookingStore();
        var booking = Booking.Create(Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
        store.AddBooking(booking);

        booking.Status = BookingStatus.Rejected;
        store.UpdateBooking(booking);
        var result = store.GetBooking(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Rejected, result!.Status);
    }
}
