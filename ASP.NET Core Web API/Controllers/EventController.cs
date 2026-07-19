using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Web_API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EventController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<Event>>  GetEvents()
    {
        return eventService.GetEvents();
    }

    [HttpGet("{eventId:int}")]
    public ActionResult<Event> GetEvent(Guid eventId)
    {
        var ev = eventService.GetEventById(eventId);    
        return ev is null ? Problem(statusCode: StatusCodes.Status404NotFound, title:"Event not Found"): Ok(ev);    
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
    public IActionResult PutEvent(Guid eventId, EventDTO eventDto)
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
        return result is null ? Problem(statusCode: StatusCodes.Status404NotFound, title:"Event not Found") : Ok(result);   
    }

    [HttpDelete("{eventId:int}")]  
    public IActionResult DeleteEvent(Guid eventId)
    {
        var result = eventService.DeleteEvent(eventId);
        return result is false ? Problem(statusCode: StatusCodes.Status404NotFound, title:"Event not Found") : Ok(result);
    }
    
}