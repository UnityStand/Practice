using Events.Application.DTOs;
using Events.Domain.Entities;

namespace Events.Application.Services;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetEvents(string? title, DateTime? from, DateTime? to, int page = 1, int pageSize = 10);
    Task<Event> GetEventById(Guid id);
    Task<Event> CreateEvent(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task<Event> UpdateEvent(Guid id, string title, string? description, DateTime startAt, DateTime endAt);
    Task<bool> DeleteEvent(Guid id);
}