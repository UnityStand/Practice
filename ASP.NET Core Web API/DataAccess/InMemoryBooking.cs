using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DataAccess;

public class InMemoryBooking : IBookingStore
{
    private readonly List<Booking> _bookings = [];

    public Booking? GetBooking(int bookingId)
    {
        return _bookings.FirstOrDefault(b => b.Id == bookingId);
    }

    public Booking AddBooking(Booking booking)
    {
        booking.Id = _bookings.Count == 0 ? 1 : _bookings.Max(b => b.Id) + 1;
        _bookings.Add(booking);
        return booking;
    }

    public IEnumerable<Booking> GetBookingsPending()
    {
        return _bookings.Where(b => b.Status == BookingStatus.Pending);
    }

    public Booking UpdateBooking(Booking booking)
    {
        var index = _bookings.FindIndex(b => b.Id == booking.Id);
        if (index != -1) _bookings[index] = booking;
        return booking;
    }
}
