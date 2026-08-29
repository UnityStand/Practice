using System.ComponentModel.DataAnnotations;

namespace ASP.NET_Core_Web_API.Models;

public class Event
{

    public Guid Id { get; set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; } = 0;
    public int AvailableSeats { get; private set; }

    private Event() { }

    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        
        if (totalSeats <= 0)
            throw new ValidationException("totalSeats cannot be less or equal to zero");
        if (endAt <= startAt)
            throw new ValidationException("StartAt cannot be less or equal to endAt");
        startAt = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);                                                                         
        endAt = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);    

        return new Event
        {
            Id = Guid.NewGuid(),
            Description = description,
            Title = title,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats,
            StartAt = startAt,
            EndAt = endAt
        };
    }

    public void UpdateInfo(string title, string? description, DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
            throw new ValidationException("StartAt cannot be less or equal to endAt");
        startAt = DateTime.SpecifyKind(startAt, DateTimeKind.Utc);                                                                         
        endAt = DateTime.SpecifyKind(endAt, DateTimeKind.Utc);    
        
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;

    }
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }
        AvailableSeats -= count;
        return true;


    }

    public bool ReleaseSeats(int count = 1)
    {
        if (AvailableSeats + count > TotalSeats)
        {
            return false;
        }
        else
        {
            AvailableSeats += count;
            return true;
        }


    }
}