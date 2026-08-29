using System.ComponentModel.DataAnnotations;
using ASP.NET_Core_Web_API.Models;

namespace ASP.NET_Core_Web_API.DTOs;

public class EventResponseDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }


    public static EventResponseDto FromEntity(Event @event) => new()
    {
        EventId = @event.Id,
        Title = @event.Title,
        Description = @event.Description,
        StartAt = @event.StartAt,
        EndAt = @event.EndAt,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats
    };
}