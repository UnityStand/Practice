using System.Text.Json;
using Bookings.Application.Abstractions;
using Confluent.Kafka;
using EventApi.Contracts;
using Microsoft.Extensions.Options;

namespace Bookings.Infrastructure.Messaging;

public sealed class KafkaBookingEventPublisher : IBookingEventPublisher, IDisposable
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(5);
    private readonly IProducer<string, string> _producer;

    public KafkaBookingEventPublisher(IOptions<KafkaOptions> options)
    {
        var config = new ProducerConfig { BootstrapServers = options.Value.BootstrapServers, Acks = Acks.All };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(), Value = JsonSerializer.Serialize(message)
        };
        await _producer.ProduceAsync(Topics.BookingConfirmed, kafkaMessage, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(FlushTimeout);
        _producer.Dispose();
    }
}