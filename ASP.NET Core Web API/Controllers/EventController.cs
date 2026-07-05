using ASP.NET_Core_Web_API.DTOs;
using ASP.NET_Core_Web_API.Models;
using ASP.NET_Core_Web_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Validation;

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
    public ActionResult<Event> GetEvent(int eventId)
    {
        var ev = eventService.GetEventById(eventId);    
        return ev is null ? NotFound() : Ok(ev);    
    }

    [HttpPost]
    public IActionResult PostEvent(EventDTO EventDTO)
    {
        var newEvent = new Event
        {
            Description = EventDTO.Description,
            StartDate = EventDTO.StartDate,
            EndDate = EventDTO.EndDate,
            Title = EventDTO.Title,
        };
        eventService.CreateEvent(newEvent);
        return  CreatedAtAction(nameof(GetEvent), new { eventId = newEvent.Id }, newEvent);
    }

    [HttpPut("{eventId:int}")]
    public IActionResult PutEvent(int eventId, EventDTO EventDTO)
    {
        var updatedEvent = new Event
        {
            Description = EventDTO.Description,
            Title = EventDTO.Title,
            StartDate = EventDTO.StartDate,
            EndDate = EventDTO.EndDate,
        };
        updatedEvent.Id = eventId;    
        var result = eventService.UpdateEvent(updatedEvent);                                                                            
        return result is null ? NotFound() : Ok(result);   
    }

    [HttpDelete]
    public IActionResult DeleteEvent(int eventId)
    {
        var result = eventService.DeleteEvent(eventId);
        return result is false ? NotFound() : Ok(result);
    }
    
}