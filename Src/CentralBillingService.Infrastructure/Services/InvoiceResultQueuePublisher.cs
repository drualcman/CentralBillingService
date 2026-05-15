using Azure.Storage.Queues;

namespace CentralBillingService.Infrastructure.Services;

public sealed class InvoiceResultQueuePublisher : IInvoiceResultQueuePublisher
{
    public async Task PublishAsync(
        InvoiceResult result,
        ResultQueueConfig config,
        CancellationToken cancellationToken = default)
    {
        var client = new QueueClient(config.ConnectionString, config.QueueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var json = JsonSerializer.Serialize(result);
        await client.SendMessageAsync(json, cancellationToken);
    }
}
