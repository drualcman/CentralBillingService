namespace CentralBillingService.Infrastructure.Queue;

/// <summary>
/// Sends QR code generation jobs to an Azure Storage Queue.
/// Uses the same storage account as the blob service (QrBlobConnectionString).
/// </summary>
public sealed class AzureQueueJobQueue : IJobQueue
{
    private readonly string _connectionString;
    private readonly string _qrQueueName;
    private readonly string _invoicesQueueName;

    public AzureQueueJobQueue(IOptions<CbsOptions> options)
    {
        _connectionString = options.Value.QrBlobConnectionString;
        _qrQueueName = options.Value.QrCodeQueueName;
        _invoicesQueueName = options.Value.Invoices;
    }

    public async Task EnqueueQrAsync(GenerateInvoiceQrCommand command, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(command);
        await EnqueueAsync(_connectionString, _qrQueueName, json, cancellationToken);
    }

    public async Task EnqueuePdfAsync(GenerateInvoiceReportCommand command, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(command);
        await EnqueueAsync(_connectionString, _invoicesQueueName, json, cancellationToken);
    }

    public async Task EnqueueAsync(string connectionString, string queueName,
        string data, CancellationToken cancellationToken = default)
    {
        var client = new QueueClient(connectionString, queueName,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await client.SendMessageAsync(data, cancellationToken);
    }
}
