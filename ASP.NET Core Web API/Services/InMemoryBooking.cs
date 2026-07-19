using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class InMemoryBooking: IBookingStore
{
    private readonly List<Booking> _bookings = [];
    public Booking? GetBooking(Guid bookingId)
    {
        return _bookings.FirstOrDefault(b => b.Id == bookingId);
    }

    public Booking AddBooking(Booking booking)
    {
        _bookings.Add(booking);
        return booking;
    }

    public IEnumerable<Booking> GetBookingsPending()
    {
       return _bookings.Where(b => b.Status == BookingStatus.Pending);
    }
}