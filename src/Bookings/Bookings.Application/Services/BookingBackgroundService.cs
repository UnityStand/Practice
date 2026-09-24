using Bookings.Application.Abstractions;
using Bookings.Domain.Entities;
using EventApi.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Services;

public class BookingBackgroundService(IBookingEventPublisher publisher,IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger) : BackgroundService
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
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                pendingBookingsIds = await bookingRepository.GetPendingIdsAsync();
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
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();


            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking is null || booking.Status != BookingStatus.Pending) return;

            booking.Reject();
            
            await bookingRepository.UpdateAsync(booking);
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
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();


            var booking = await bookingRepository.GetByIdAsync(bookingId);
            if (booking is null || booking.Status != BookingStatus.Pending) return;

            booking.Confirm();
            await bookingRepository.UpdateAsync(booking);
            await publisher.PublishAsync(new BookingConfirmed(                                                                             
                    BookingId:   booking.Id,                                                                                               
                    EventId:     booking.EventId,                                                                                          
                    UserId:      booking.UserId,                                                                                           
                    Confirmed:   booking.ProcessedAt!.Value,                                                                               
                    BookedSeats: booking.Seats),                                                                                           
                stoppingToken);     
            logger.LogInformation("Booking {BookingId} confirmed", booking.Id);

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
