using Events.Application.DTOs;
using Events.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Api.Controllers;

[ApiController]
[Route("events")]
public class EventController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<EventResponseDto>>> GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var result = await eventService.GetEvents(title, from, to, page, pageSize);
        return new PaginatedResult<EventResponseDto>
        {
            TotalCount = result.TotalCount,
            Items = result.Items.Select(EventResponseDto.FromEntity).ToList(),
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    [HttpGet("top")]
    public async Task<ActionResult<List<EventResponseDto>>> GetTopEvents()
    {
        return Ok(await eventService.GetTopEvents());
    }

    [HttpGet("{eventId:Guid}")]
    public async Task<ActionResult<EventResponseDto>> GetEvent(Guid eventId)
    {
        var ev = await eventService.GetEventById(eventId);
        return Ok(ev);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PostEvent(CreateEventDto createEventDto)
    {

        var newEvent = await eventService.CreateEvent(createEventDto.Title, createEventDto.Description, createEventDto.StartAt, createEventDto.EndAt, createEventDto.TotalSeats!.Value);
        return CreatedAtAction(nameof(GetEvent), new { eventId = newEvent.EventId },newEvent);
    }

    [HttpPut("{eventId:Guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutEvent(Guid eventId, EventRequestDto dto)
    {

        var result = await eventService.UpdateEvent(eventId, dto.Title, dto.Description, dto.StartAt, dto.EndAt);

        return Ok(result);
    }

    [HttpDelete("{eventId:Guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        await eventService.DeleteEvent(eventId);
        return NoContent();
    }


}