namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Enqueues a QR code generation job for async processing.
/// The actual QR image is generated and uploaded by a background worker
/// (Azure Function queue trigger) after the invoice has been persisted.
/// </summary>
public interface IQrCodeJobQueue
{
    Task EnqueueAsync(GenerateInvoiceQrCommand command, CancellationToken cancellationToken = default);
}
