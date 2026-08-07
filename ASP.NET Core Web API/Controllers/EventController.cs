using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Controllers;

[ApiController]
[Route("events")]
public class EventController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public ActionResult<PaginatedResult<Event>> GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        return eventService.GetEvents(title, from, to, page, pageSize);
    }

    [HttpGet("{eventId:Guid}")]
    public ActionResult<Event> GetEvent(Guid eventId)
    {
        var ev = eventService.GetEventById(eventId);
        return Ok(ev);
    }

    [HttpPost]
    public IActionResult PostEvent(CreateEventDto createEventDto)
    {

        var newEvent = eventService.CreateEvent(createEventDto.Title, createEventDto.Description, createEventDto.StartAt, createEventDto.EndAt, createEventDto.TotalSeats!.Value);
        return CreatedAtAction(nameof(GetEvent), new { eventId = newEvent.Id }, newEvent);
    }

    [HttpPut("{eventId:Guid}")]
    public IActionResult PutEvent(Guid eventId, EventInfoDto createEventDto)
    {
        var updatedEvent = MapDtoToEvent(createEventDto);
        updatedEvent.Id = eventId;
        var result = eventService.UpdateEvent(updatedEvent);
        return Ok(result);
    }

    [HttpDelete("{eventId:Guid}")]
    public IActionResult DeleteEvent(Guid eventId)
    {
        eventService.DeleteEvent(eventId);
        return NoContent();
    }

    private static Event MapDtoToEvent(EventInfoDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        StartAt = dto.StartAt,
        EndAt = dto.EndAt,

    };

}