namespace CentralBillingService.Application.Exceptions;

/// <summary>
/// Thrown when an invoice cannot be found by its number or ID.
/// The caller (Azure Function, controller) maps this to a 404 response.
/// </summary>
public sealed class InvoiceNotFoundException : DomainException
{
    public string InvoiceNumber { get; }

    public InvoiceNotFoundException(string invoiceNumber)
        : base($"Invoice '{invoiceNumber}' not found.")
    {
        InvoiceNumber = invoiceNumber;
    }
}

/// <summary>
/// Thrown by the persistence layer when a concurrent request tries to persist a second
/// invoice with the same BillingSource + PaymentReference (the unique index rejects it).
/// The create use case catches this, re-reads the already-persisted invoice and returns it,
/// so a retried/concurrent payment webhook stays idempotent instead of failing.
/// </summary>
public sealed class DuplicatePaymentReferenceException : DomainException
{
    public string BillingSource { get; }
    public string PaymentReference { get; }

    public DuplicatePaymentReferenceException(string billingSource, string paymentReference, Exception inner)
        : base($"An invoice already exists for billing source '{billingSource}' and payment reference '{paymentReference}'.", inner)
    {
        BillingSource = billingSource;
        PaymentReference = paymentReference;
    }
}