using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class BookingProcessingService(IBookingStore bookingsStore ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var booking in bookingsStore.GetBookingsPending().ToList())
            {
                await Task.Delay(1000, stoppingToken);
                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.Now; // для простоты использую текущее время сервера, в проде так не буду 
                bookingsStore.UpdateBooking(booking);

            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}