using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASP.NET_Core_Web_API.Services;

public class BookingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private const int PollingIntervalMs = 1000;
    private const int ProcessingDelayMs = 1000;
    private static readonly int MaxConcurrentProcessing = Environment.ProcessorCount;
    private readonly SemaphoreSlim _processingSemaphore = new(MaxConcurrentProcessing, MaxConcurrentProcessing);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            List<Guid>? pendingBookingsIds = null;
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                pendingBookingsIds = await context.Bookings
                    .Where(b => b.Status == BookingStatus.Pending)
                    .Select(b => b.Id)
                    .ToListAsync(stoppingToken);
            }
            var tasks = pendingBookingsIds.Select(booking => ProcessBookingAsync(booking, stoppingToken));
            await Task.WhenAll(tasks);
            await Task.Delay(PollingIntervalMs, stoppingToken);



        }
    }

    private async Task CompensateAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings.FindAsync(bookingId);
            if (booking is null || booking.Status == BookingStatus.Confirmed) return;

            booking.Reject();
            var @event = await context.Events.FindAsync(booking.EventId);
            @event?.ReleaseSeats();
            await context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception compensationError)
        {
            logger.LogError(compensationError, "Failed to compensate booking {BookingId} after processing error", bookingId);
        }
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        await Task.Delay(ProcessingDelayMs, stoppingToken);


        var acquired = false;

        try
        {
            await _processingSemaphore.WaitAsync(stoppingToken);
            acquired = true;

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings.FindAsync(bookingId);
            if (booking is null) return;

            var @event = await context.Events.FindAsync(booking.EventId);
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
            await context.SaveChangesAsync(stoppingToken);

        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unexpected error while processing booking {BookingId}", bookingId);
            await CompensateAsync(bookingId, stoppingToken);
        }
        finally
        {
            if (acquired) _processingSemaphore.Release();
        }

    }
}
