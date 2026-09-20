using System.ComponentModel.DataAnnotations;

namespace EventApi.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    private Booking() { }
    public Event Event { get; private set; } = null!;

    public static Booking Create(Guid eventId, Guid userId, BookingStatus status, DateTime createdAt)
    {
        return new Booking
        {

            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = status,
            CreatedAt = createdAt,

        };
    }
    public void Confirm()
    {
        if (Status == BookingStatus.Confirmed)
            throw new ValidationException("Booking is already confirmed");
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != BookingStatus.Rejected)
            throw new ValidationException("The booking is already rejected");
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new ValidationException("Booking is already cancelled");
        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
