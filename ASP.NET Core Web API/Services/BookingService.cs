using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

internal class BookingService(IBookingStore bookingStore, IEventService eventService) : IBookingService
{
    private readonly object _bookingLock = new();

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var @event = eventService.GetEventById(eventId);
        lock (_bookingLock)
        {

            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
            var booking = Booking.Create(eventId, BookingStatus.Pending, DateTime.UtcNow);

            bookingStore.AddBooking(booking);

            return Task.FromResult(booking);
        }

    }

    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = bookingStore.GetBooking(bookingId);
        if (booking is null) throw new NotFoundException($"Booking with id {bookingId} not found");

        return Task.FromResult(booking);
    }
}