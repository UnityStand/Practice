namespace Events.Infrastructure.Messaging;

public class KafkaOptions                                                                                                          
{                                                                                                                                  
    public string BootstrapServers { get; set; } = string.Empty;
    public string ConsumerGroup { get; set; } = string.Empty;  
}   