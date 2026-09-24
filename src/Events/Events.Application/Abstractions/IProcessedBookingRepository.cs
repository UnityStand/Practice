using Events.Domain.Entities;

namespace Events.Application.Abstractions;

public interface IProcessedBookingRepository
{
    Task<bool> ExistsAsync(Guid bookingId);                                                                                            
    Task<bool> AnyForEventAsync(Guid eventId);                                                                                         
    void Add(ProcessedBooking processedBooking); 
}