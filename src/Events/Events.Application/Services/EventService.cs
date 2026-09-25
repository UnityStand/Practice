using Events.Application.Abstractions;
using Events.Application.Caching;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Events.Application.Services;

public class EventService(IEventRepository
    eventRepository,IProcessedBookingRepository processedBookingRepository,ICacheService cache, IOptions<CacheOptions> options) : IEventService
{
    private const int TopEventsCount = 10;

    private async Task<Event> FindEventOrThrow(Guid id)
    {
        var result = await eventRepository.GetEventByIdAsync(id);
        return result ?? throw new NotFoundException($"Event with id {id} not found");
    }

    public async Task<PaginatedResult<Event>> GetEvents(string? title, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 10)
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

    public async Task<EventResponseDto> GetEventById(Guid id)
    {
        var key = CacheKeys.Event(id);
        var cached = await cache.GetAsync<EventResponseDto>(key);
        if (cached is not null) return cached;

        var dto = EventResponseDto.FromEntity(await FindEventOrThrow(id));
        await cache.SetAsync(key, dto, options.Value.EventTtl);
        return dto;
    }

    public async Task<EventResponseDto> CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        var newEvent = Event.Create(title, description, startAt, endAt, totalSeats);
        await eventRepository.AddAsync(newEvent);

        return EventResponseDto.FromEntity(newEvent) ;
    }

    public async Task<EventResponseDto> UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {

        var existingEvent = await FindEventOrThrow(id);
        existingEvent.UpdateInfo(title, description, startAt, endAt);

        await eventRepository.UpdateAsync(existingEvent);
        await cache.DeleteAsync(CacheKeys.Event(id));
        return EventResponseDto.FromEntity(existingEvent);
    }

    public async Task<bool> DeleteEvent(Guid id)
    {   
        var existingEvent = await FindEventOrThrow(id);
        if (await processedBookingRepository.AnyForEventAsync(id))                                                                           
            throw new EventHasBookingsException("Cannot delete event with any bookings");   
        await eventRepository.RemoveAsync(existingEvent);
        await cache.DeleteAsync(CacheKeys.Event(id));
        return true;
    }

    public async Task<List<EventResponseDto>> GetTopEvents()
    {
        var cached = await cache.GetAsync<List<EventResponseDto>>(CacheKeys.TopEvents);
        if (cached is not null) return cached;

        var events = await eventRepository.GetTopAsync(TopEventsCount);
        var dtos = events.Select(EventResponseDto.FromEntity).ToList();
        await cache.SetAsync(CacheKeys.TopEvents, dtos, options.Value.TopEventsTtl);
        return dtos;
    }
}
