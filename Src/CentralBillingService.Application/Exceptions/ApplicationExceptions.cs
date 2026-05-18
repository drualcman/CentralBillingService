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