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
    public IActionResult PostEvent(Event newEvent)
    {
        eventService.CreateEvent(newEvent);
        return  CreatedAtAction(nameof(GetEvent), new { eventId = newEvent.Id }, newEvent);
    }

    [HttpPut("{eventId:int}")]
    public IActionResult PutEvent(int eventId, Event updatedEvent)
    {
        updatedEvent.Id = eventId;    
        var result = eventService.UpdateEvent(updatedEvent);                                                                            
        return result is null ? NotFound() : Ok(result);   
    }

    [HttpDelete("{eventId:int}")]
    public IActionResult DeleteEvent(int eventId)
    {
        var result = eventService.DeleteEvent(eventId);
        return result is false ? NotFound() : Ok(result);
    }
    
}