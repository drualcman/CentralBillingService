namespace CentralBillingService.Application.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on an invoice
/// that is not in the required status.
/// The caller maps this to a 409 Conflict response.
/// </summary>
public sealed class InvalidInvoiceStatusException : Exception
{
    public InvalidInvoiceStatusException(string invoiceNumber, string currentStatus, string requiredStatus)
        : base($"Invoice '{invoiceNumber}' is in status '{currentStatus}', expected '{requiredStatus}'.")
    {
    }
}
