using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Bookings.Domain.Entities;

namespace Bookings.Application.DTOs;

public class BookingResponseDto
{
    [Required] public Guid BookingId { get; set; }
    [Required] public Guid EventId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Required] public BookingStatus Status { get; set; }
    [Required] public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public static BookingResponseDto FromEntity(Booking booking) => new()
    {
        BookingId = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}
