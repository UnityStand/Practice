using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;

namespace ASP.NET_Core_Web_API.Tests;

public class BookingServiceTests
{
    private static (BookingService bookingService, IEventService eventService, IBookingStore bookingStore) CreateSut()
    {
        var eventService = new EventService();
        var bookingStore = new InMemoryBooking();
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
}

public class InMemoryBookingTests
{
    [Fact]
    public void GetBookingsPending_ExcludesConfirmedBookings()
    {
        var store = new InMemoryBooking();
        var pending = new Booking { EventId = Guid.NewGuid(), Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow };
        var confirmed = new Booking { EventId = Guid.NewGuid(), Status = BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow };
        store.AddBooking(pending);
        store.AddBooking(confirmed);

        var result = store.GetBookingsPending().ToList();

        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
    }

    [Fact]
    public void UpdateBooking_PersistsStatusChange()
    {
        var store = new InMemoryBooking();
        var booking = new Booking { EventId = Guid.NewGuid(), Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow };
        store.AddBooking(booking);

        booking.Status = BookingStatus.Rejected;
        store.UpdateBooking(booking);
        var result = store.GetBooking(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Rejected, result!.Status);
    }
}
