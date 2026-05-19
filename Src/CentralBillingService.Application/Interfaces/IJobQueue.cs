namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Enqueues a QR code generation job for async processing.
/// The actual QR image is generated and uploaded by a background worker
/// (Azure Function queue trigger) after the invoice has been persisted.
/// </summary>
public interface IJobQueue
{
    Task EnqueueQrAsync(GenerateInvoiceQrCommand command, CancellationToken cancellationToken = default);
    Task EnqueuePdfAsync(GenerateInvoiceReportCommand command, CancellationToken cancellationToken = default);
    Task EnqueueAsync(string connectionString, string queueName,
        string data, CancellationToken cancellationToken = default);
}
