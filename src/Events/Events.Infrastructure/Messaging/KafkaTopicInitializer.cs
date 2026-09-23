using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventApi.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Messaging;

public class KafkaTopicInitializer(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    private const int Partitions = 3;
    private const short ReplicationFactor = 1;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new AdminClientConfig { BootstrapServers = options.Value.BootstrapServers };
        using var admin = new AdminClientBuilder(config).Build();
        var topic = new TopicSpecification
        {
            Name = Topics.BookingConfirmed,
            NumPartitions = Partitions,
            ReplicationFactor = ReplicationFactor
        };
        try
        {
            await admin.CreateTopicsAsync([topic],
                new CreateTopicsOptions
                {
                    RequestTimeout = RequestTimeout
                });
            logger.LogInformation("Topic {Topic} created", topic.Name);
        }
        catch (CreateTopicsException e) when (e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Topic {Topic} already exists", topic.Name);       
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to create topics");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)=> Task.CompletedTask; 
}
