namespace ASP.NET_Core_Web_API.Services;

public interface IBookingService
{
   Task CreateBookingAsync(Guid eventId);
   Task  GetBookingByIdAsync(Guid bookingId);
}