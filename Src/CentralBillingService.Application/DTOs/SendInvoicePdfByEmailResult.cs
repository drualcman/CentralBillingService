namespace CentralBillingService.Application.DTOs;

public sealed record SendInvoicePdfByEmailResult
{
    public bool Success { get; init; }
    public string Message { get; init; }

    private SendInvoicePdfByEmailResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static SendInvoicePdfByEmailResult Sent(string recipientEmail) =>
        new(true, $"Invoice sent successfully to {recipientEmail}.");

    public static SendInvoicePdfByEmailResult NoEmail(string recipientName) =>
        new(false, $"Recipient '{recipientName}' has no email address configured. Please contact support.");

    public static SendInvoicePdfByEmailResult PdfNotFound(string invoiceNumber) =>
        new(false, $"The PDF for invoice '{invoiceNumber}' was not found. Please contact support.");
}
