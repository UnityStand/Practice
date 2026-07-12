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
    public ActionResult<PaginatedResult<Event>>  GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)    
    {
        return eventService.GetEvents(title, from, to, page, pageSize);
    }

    [HttpGet("{eventId:int}")]
    public ActionResult<Event> GetEvent(int eventId)
    {
        var ev = eventService.GetEventById(eventId);    
        return  Ok(ev);    
    }

    [HttpPost]
    public IActionResult PostEvent(EventDTO eventDto)
    {
        var newEvent = new Event
        {
            Description = eventDto.Description,
            StartAt = eventDto.StartAt,
            EndAt = eventDto.EndAt,
            Title = eventDto.Title,
        };
        eventService.CreateEvent(newEvent);
        return  CreatedAtAction(nameof(GetEvent), new { eventId = newEvent.Id }, newEvent);
    }

    [HttpPut("{eventId:int}")]
    public IActionResult PutEvent(int eventId, EventDTO eventDto)
    {
        var updatedEvent = new Event
        {
            Description = eventDto.Description,
            Title = eventDto.Title,
            StartAt = eventDto.StartAt,
            EndAt = eventDto.EndAt,
        };
        updatedEvent.Id = eventId;    
        var result = eventService.UpdateEvent(updatedEvent);                                                                            
        return Ok(result);   
    }

    [HttpDelete("{eventId:int}")]  
    public IActionResult DeleteEvent(int eventId)
    {
        var result = eventService.DeleteEvent(eventId);
        return  NoContent();
    }
    
}