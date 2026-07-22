using ASP.NET_Core_Web_API.DataAccess;
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

    private static Event CreateEvent(IEventService eventService, string title = "Test Event")
    {
        return eventService.CreateEvent(new Event
        {
            Title = title,
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2)
        });
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsPendingBooking_WhenEventExists()
    {
        // Arrange
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateEvent(eventService);

        // Act
        var booking = await bookingService.CreateBookingAsync(ev.Id);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(ev.Id, booking!.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotEqual(Guid.Empty, booking.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_AssignsUniqueIds_ForMultipleBookings()
    {
        // Arrange
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateEvent(eventService);

        // Act
        var first = await bookingService.CreateBookingAsync(ev.Id);
        var second = await bookingService.CreateBookingAsync(ev.Id);

        // Assert
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Id, second!.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsBooking_WhenExists()
    {
        // Arrange
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateEvent(eventService);
        var created = await bookingService.CreateBookingAsync(ev.Id);

        // Act
        var result = await bookingService.GetBookingByIdAsync(created!.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReflectsStatusChange_AfterUpdate()
    {
        // Arrange
        var (bookingService, eventService, bookingStore) = CreateSut();
        var ev = CreateEvent(eventService);
        var created = await bookingService.CreateBookingAsync(ev.Id);
        created!.Status = BookingStatus.Confirmed;
        created.ProcessedAt = DateTime.UtcNow;
        bookingStore.UpdateBooking(created);

        // Act
        var result = await bookingService.GetBookingByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result!.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsNull_WhenEventDoesNotExist()
    {
        // Arrange
        var (bookingService, _, _) = CreateSut();

        // Act
        var result = await bookingService.CreateBookingAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsNull_WhenEventWasDeleted()
    {
        // Arrange
        var (bookingService, eventService, _) = CreateSut();
        var ev = CreateEvent(eventService);
        eventService.DeleteEvent(ev.Id);

        // Act
        var result = await bookingService.CreateBookingAsync(ev.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsNull_WhenBookingDoesNotExist()
    {
        // Arrange
        var (bookingService, _, _) = CreateSut();

        // Act
        var result = await bookingService.GetBookingByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}

public class InMemoryBookingTests
{
    [Fact]
    public void GetBookingsPending_ExcludesConfirmedBookings()
    {
        // Arrange
        var store = new InMemoryBooking();
        var pending = new Booking { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow };
        var confirmed = new Booking { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), Status = BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow };
        store.AddBooking(pending);
        store.AddBooking(confirmed);

        // Act
        var result = store.GetBookingsPending().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
    }

    [Fact]
    public void UpdateBooking_PersistsStatusChange()
    {
        // Arrange
        var store = new InMemoryBooking();
        var booking = new Booking { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow };
        store.AddBooking(booking);

        // Act
        booking.Status = BookingStatus.Rejected;
        store.UpdateBooking(booking);
        var result = store.GetBooking(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Rejected, result!.Status);
    }
}
