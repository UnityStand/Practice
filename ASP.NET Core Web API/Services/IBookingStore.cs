using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public interface IBookingStore
{
    Booking? GetBooking(Guid bookingId);
    Booking AddBooking(Booking booking);
    IEnumerable<Booking> GetBookingsPending();
}