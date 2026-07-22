using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Controllers;
[ApiController]

public class BookingController(IBookingService bookingService): ControllerBase
{
    [HttpGet("/bookings/{bookingId:Guid}")]
    public  async Task<ActionResult<BookingResponseDto>>   GetBooking(Guid bookingId)
    {
        var rawResult = await bookingService.GetBookingByIdAsync(bookingId);
        if (rawResult is null) return Problem(statusCode: StatusCodes.Status404NotFound, title: "Booking not Found");

        var result = BookingResponseDto.FromEntity(rawResult);
        
        return Ok(result);  
    }

    [HttpPost("/events/{eventId:Guid}/book")]      
    public async Task<ActionResult<BookingResponseDto>> PostBooking(Guid eventId)
    {
        var rawResult = await bookingService.CreateBookingAsync(eventId);
        if (rawResult is null) return Problem(statusCode: StatusCodes.Status404NotFound, title: "Event not Found");
        var result = BookingResponseDto.FromEntity(rawResult);
        return AcceptedAtAction(nameof(GetBooking), new { bookingId = rawResult.Id }, result);
    }
}