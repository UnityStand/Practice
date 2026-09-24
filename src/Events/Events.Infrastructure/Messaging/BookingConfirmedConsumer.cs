using System.Text.Json;
using Confluent.Kafka;
using EventApi.Contracts;
using Events.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Messaging;

public sealed class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);


    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest, EnableAutoCommit = false
        };
        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics.BookingConfirmed);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException e)
                {
                    logger.LogWarning(e, "Error while reading");
                    continue;
                }

                if (await HandleMessageAsync(result, stoppingToken))
                {
                    consumer.Commit(result);
                }
                else
                {
                    consumer.Seek(result.TopicPartitionOffset);
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Application shutdown");
        }
        finally
        {
            consumer.Close();
        }
    }


    private async Task<bool> HandleMessageAsync(ConsumeResult<string, string> result, CancellationToken stoppingToken)
    {
        BookingConfirmed? handledMessage = null;
        try
        {
            handledMessage = JsonSerializer.Deserialize<BookingConfirmed>(result.Message.Value);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Invalid message at {Offset}, skipping", result.TopicPartitionOffset);
            return true;
        }

        if (handledMessage == null)
        {
            logger.LogWarning("Message is null");
            return true;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IBookingConfirmedHandler>();
            await handler.HandleAsync(handledMessage);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to handle message at {Offset}, will retry", result.TopicPartitionOffset);
            return false;
        }
    }
}