using System.Collections.Concurrent;
using Bookings.Application.Abstractions;
using Bookings.Domain.Entities;
using Bookings.Infrastructure.Persistence;
using EventApi.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Tests;

// Фейк вместо Kafka: запоминает сообщения и статус брони В БАЗЕ в момент публикации —
// так тест может проверить порядок «сначала сохранили, потом опубликовали».
public sealed class RecordingPublisher : IBookingEventPublisher
{
    private readonly TaskCompletionSource _firstPublished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IServiceScopeFactory ScopeFactory { get; set; } = null!;

    public ConcurrentQueue<(BookingConfirmed Message, BookingStatus? StatusInDb)> Published { get; } = new();

    public async Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var saved = await context.Bookings.FindAsync([message.BookingId], cancellationToken);

        Published.Enqueue((message, saved?.Status));
        _firstPublished.TrySetResult();
    }

    public Task WaitForFirstAsync(TimeSpan timeout) => _firstPublished.Task.WaitAsync(timeout);
}
