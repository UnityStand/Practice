using Events.Application.Abstractions;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Domain.Exceptions;

namespace Events.Application.Services;

public class EventService(IEventRepository
    eventRepository,IProcessedBookingRepository processedBookingRepository) : IEventService
{

    private async Task<Event> FindEventOrThrow(Guid id)
    {
        var result = await eventRepository.GetEventByIdAsync(id);
        return result ?? throw new NotFoundException($"Event with id {id} not found");
    }

    public async Task<PaginatedResult<Event>> GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
    {
        var result = await eventRepository.GetPagedAsync(title, from, to, page, pageSize);
        return new PaginatedResult<Event>
        {
            TotalCount = result.TotalCount,
            Items = result.Items,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Event> GetEventById(Guid id)
    {
        return await FindEventOrThrow(id);
    }


    public async Task<Event> CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var newEvent = Event.Create(title, description, startAt, endAt, totalSeats);
        await eventRepository.AddAsync(newEvent);

        return newEvent;
    }

    public async Task<Event> UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {

        var existingEvent = await FindEventOrThrow(id);
        existingEvent.UpdateInfo(title, description, startAt, endAt);

        await eventRepository.UpdateAsync(existingEvent);

        return existingEvent;
    }

    public async Task<bool> DeleteEvent(Guid id)
    {   
        var existingEvent = await FindEventOrThrow(id);
        if (await processedBookingRepository.AnyForEventAsync(id))                                                                           
            throw new EventHasBookingsException("Cannot delete event with any bookings");   
        await eventRepository.RemoveAsync(existingEvent);

        return true;
    }
}