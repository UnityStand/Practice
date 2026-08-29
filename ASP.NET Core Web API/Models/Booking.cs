using ASP.NET_Core_Web_API.DTOs;

namespace ASP.NET_Core_Web_API.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    private Booking() { }
    public Event Event { get; private set; } = null!;

    public static Booking Create(Guid eventId, BookingStatus status, DateTime createdAt)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = status,
            CreatedAt = createdAt,

        };
    }
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}
