namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Port for dispatching domain events after a successful use case execution.
/// Keeps the use case decoupled from email sending, PDF generation,
/// webhook notifications, VeriFactu submission, etc.
/// </summary>
public interface IInvoiceEventDispatcher
{
    /// <summary>
    /// Called after an invoice has been persisted successfully.
    /// Handlers can generate the PDF, send the email, notify VeriFactu, etc.
    /// Failures in handlers must not roll back the invoice — they are logged
    /// and retried independently.
    /// </summary>
    Task InvoiceCreatedAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task InvoiceRectifiedAsync(
        RectificativeInvoice rectificative,
        Invoice updatedOriginal,
        CancellationToken cancellationToken = default);
}
