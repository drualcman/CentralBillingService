namespace CentralBillingService.Domain.Models;

public sealed class ResultQueueConfig
{
    public required string ConnectionString { get; init; }
    public required string QueueName { get; init; }
}
