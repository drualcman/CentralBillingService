namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Result of an admin-side integrity check (no document hash required).
/// Confirms that the invoice stored in the DB has not been modified since it was issued.
/// </summary>
public sealed class CheckIntegrityResult
{
    public required string InvoiceNumber { get; init; }
    public required bool IsValid { get; init; }
    public required string Hash { get; init; }
    public string? Message { get; init; }
}
