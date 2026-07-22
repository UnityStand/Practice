using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class BookingProcessingService(IBookingStore bookingsStore, ILogger<BookingProcessingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var booking in bookingsStore.GetBookingsPending().ToList())
            {
                try
                {
                    await Task.Delay(1000, stoppingToken);
                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.Now; // для простоты использую текущее время сервера, в проде так не буду
                    bookingsStore.UpdateBooking(booking);
                    logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process booking {BookingId}", booking.Id);
                }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}