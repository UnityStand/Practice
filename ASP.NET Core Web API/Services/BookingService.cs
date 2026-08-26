using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Exceptions;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

internal class BookingService(AppDbContext context) : IBookingService
{
    private static readonly SemaphoreSlim _bookingLock = new(1, 1);
    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {   await _bookingLock.WaitAsync();

        try
        {
            var @event = await context.Events.FindAsync(eventId);
            if (@event == null) throw new NotFoundException($"Event with id {eventId} not found");
            if (!@event.TryReserveSeats()) throw new NoAvailableSeatsException("No available seats for this event");
            var booking = Booking.Create(eventId, BookingStatus.Pending, DateTime.UtcNow);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();
            return booking;

        }
        finally
        {
            _bookingLock.Release();      
        }

        
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await context.Bookings.FindAsync(bookingId);
        return booking ?? throw new NotFoundException($"Booking with id {bookingId} not found");
    }
}