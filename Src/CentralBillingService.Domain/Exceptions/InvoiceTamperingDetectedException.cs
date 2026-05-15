namespace CentralBillingService.Domain.Exceptions;

/// <summary>
/// Thrown when an invoice's stored hash does not match its recomputed hash,
/// indicating possible external tampering with the persisted data.
/// </summary>
public sealed class InvoiceTamperingDetectedException : Exception
{
    public string InvoiceNumber { get; }
    public string StoredHash { get; }

    public InvoiceTamperingDetectedException(string invoiceNumber, string storedHash)
        : base($"Invoice '{invoiceNumber}' integrity check failed: the stored hash does not match the recomputed hash. Possible tampering detected.")
    {
        InvoiceNumber = invoiceNumber;
        StoredHash = storedHash;
    }
}
