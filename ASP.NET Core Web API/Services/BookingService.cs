namespace ASP.NET_Core_Web_API.Services;

public class BookingService : IBookingService
{
    Task CreateBookingAsync(Guid eventId)
    {
        return Task.CompletedTask;
    }

    Task  GetBookingByIdAsync(Guid bookingId)
    {
        return Task.CompletedTask;
    }
}