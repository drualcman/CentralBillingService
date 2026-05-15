using Azure.Storage.Queues;

namespace CentralBillingService.Infrastructure.Queue;

/// <summary>
/// Sends QR code generation jobs to an Azure Storage Queue.
/// Uses the same storage account as the blob service (QrBlobConnectionString).
/// </summary>
public sealed class AzureQueueQrCodeJobQueue : IQrCodeJobQueue
{
    private readonly string _connectionString;
    private readonly string _queueName;

    public AzureQueueQrCodeJobQueue(IOptions<CbsOptions> options)
    {
        _connectionString = options.Value.QrBlobConnectionString;
        _queueName = options.Value.QrCodeQueueName;
    }

    public async Task EnqueueAsync(GenerateInvoiceQrCommand command, CancellationToken cancellationToken = default)
    {
        var client = new QueueClient(_connectionString, _queueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var json = JsonSerializer.Serialize(command);
        await client.SendMessageAsync(json, cancellationToken);
    }
}
