namespace CentralBillingService.Domain.Models;

public sealed class ResultQueueConfig
{
    public string ConnectionString { get; set; } = "";
    public string QueueName { get; set; } = "";
}
