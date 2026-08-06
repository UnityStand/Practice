using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Controllers;

[ApiController]
public class BookingController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("/bookings/{bookingId:int}")]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(int bookingId)
    {
        var booking = await bookingService.GetBookingByIdAsync(bookingId);
        return Ok(BookingResponseDto.FromEntity(booking));
    }

    [HttpPost("/events/{eventId:int}/book")]
    public async Task<ActionResult<BookingResponseDto>> PostBooking(int eventId)
    {
        var booking = await bookingService.CreateBookingAsync(eventId);
        var result = BookingResponseDto.FromEntity(booking);
        return AcceptedAtAction(nameof(GetBooking), new { bookingId = booking.Id }, result);
    }
}
