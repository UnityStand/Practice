using ASP.NET_Core_Web_API.DataAccess;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.Services;

public class BookingBackgroundService(IBookingStore bookingStore,IEventStore eventStore ,  ILogger<BookingBackgroundService> logger) : BackgroundService
{
    private const int PollingIntervalMs = 1000;                                            
    private const int ProcessingDelayMs = 1000;   
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1); 
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
        
        await _processingSemaphore.WaitAsync(stoppingToken);
        try
        {
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
            booking.Reject();                                                            
            @event?.ReleaseSeats();                                                      
            bookingStore.UpdateBooking(booking);                                         
            logger.LogError(e, "Unexpected error while processing booking {BookingId}",  
                booking.Id);   
        }
        finally
        {
            _processingSemaphore.Release();  
        }
        
    }
}
