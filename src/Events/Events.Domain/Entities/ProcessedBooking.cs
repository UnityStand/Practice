namespace Events.Domain.Entities;

public class ProcessedBooking                                                                                                      
{                                                                                                                                  
    public Guid BookingId { get; private set; }                                                              
    public Guid EventId { get; private set; }                                                                                      
    public DateTime ProcessedAt { get; private set; }                                                                              
                                                                                                                                     
    private ProcessedBooking() { }                                                                                                 
    public static ProcessedBooking Create(Guid bookingId, Guid eventId, DateTime processedAt) => new()
    {
        BookingId = bookingId,
        EventId = eventId,
        ProcessedAt = processedAt
    };                                                       
}  