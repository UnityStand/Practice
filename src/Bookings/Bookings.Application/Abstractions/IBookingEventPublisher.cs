using EventApi.Contracts;

namespace Bookings.Application.Abstractions;

public interface IBookingEventPublisher                                                                                            
{                                                                                                                                  
    Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken);                                              
}      