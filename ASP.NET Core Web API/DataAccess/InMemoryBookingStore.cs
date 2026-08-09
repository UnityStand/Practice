using System.Collections.Concurrent;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DataAccess;

public class InMemoryBookingStore : IBookingStore
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = [];

    public Booking? GetBooking(Guid bookingId)
    {
        return _bookings.GetValueOrDefault(bookingId);
    }

    public Booking AddBooking(Booking booking)
    {
        booking.Id = Guid.NewGuid();
        _bookings[booking.Id] = booking;
        return booking;
    }

    public IEnumerable<Booking> GetBookingsPending()
    {
        return _bookings.Values.Where(b => b.Status == BookingStatus.Pending);
    }

    public Booking UpdateBooking(Booking booking)
    {
        _bookings[booking.Id] = booking;
        return booking;
    }
}
