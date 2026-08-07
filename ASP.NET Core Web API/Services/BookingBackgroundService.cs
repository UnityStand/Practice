using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class BookingBackgroundService(IBookingStore bookingStore, IEventStore eventStore, ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private const int PollingIntervalMs = 1000;
    private const int ProcessingDelayMs = 1000;
    private static readonly int MaxConcurrentProcessing = Environment.ProcessorCount;
    private readonly SemaphoreSlim _processingSemaphore = new(MaxConcurrentProcessing, MaxConcurrentProcessing);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = bookingStore.GetBookingsPending().ToList();
            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
            await Task.WhenAll(tasks);
            await Task.Delay(PollingIntervalMs, stoppingToken);

        }
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(ProcessingDelayMs, stoppingToken);

        Event? @event = null;
        var acquired = false;

        try
        {
            await _processingSemaphore.WaitAsync(stoppingToken);
            acquired = true;

            @event = eventStore.Get(booking.EventId);
            if (@event is not null)
            {
                booking.Confirm();
                logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
            }
            else
            {
                booking.Reject();
                logger.LogWarning("Booking {BookingId} not found , rejecting", booking.Id);
            }
            bookingStore.UpdateBooking(booking);

        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            if (acquired && booking.Status != BookingStatus.Confirmed)
            {
                booking.Reject();
                @event?.ReleaseSeats();
                bookingStore.UpdateBooking(booking);
            }
            logger.LogError(e, "Unexpected error while processing booking {BookingId}",
                booking.Id);
        }
        finally
        {
            if (acquired) _processingSemaphore.Release();
        }

    }
}
