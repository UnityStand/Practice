using System.Security.Claims;
using EventApi.Application.DTOs;
using EventApi.Application.Services;
using EventApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventApi.Presentation.Controllers;

[ApiController]
[Authorize]
public class BookingController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("/bookings/{bookingId:Guid}")]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid bookingId)
    {
        var booking = await bookingService.GetBookingByIdAsync(bookingId);
        return Ok(BookingResponseDto.FromEntity(booking));
    }

    [HttpPost("/events/{eventId:Guid}/book")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> PostBooking(Guid eventId)
    {
        var userId = GetCurrentUserId();
        var booking = await bookingService.CreateBookingAsync(eventId, userId);
        var result = BookingResponseDto.FromEntity(booking);
        return AcceptedAtAction(nameof(GetBooking), new { bookingId = booking.Id }, result);
    }

    [HttpDelete("/bookings/{bookingId:Guid}")]        
    public async Task<IActionResult> DeleteBooking(Guid bookingId)
    {
        var userId = GetCurrentUserId();                                                                     
        var userRole =GetCurrentUserRole();                          
        await bookingService.CancelBookingAsync(bookingId, userId, userRole);                                
        return NoContent();      
    }
    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private UserRole GetCurrentUserRole() =>
        Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role)!); 
}

