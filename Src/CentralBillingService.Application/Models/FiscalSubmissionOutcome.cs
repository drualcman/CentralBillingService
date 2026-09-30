namespace CentralBillingService.Application.Models;

/// <summary>
/// Result of submitting an invoice's fiscal record to the authority (e.g. AEAT VeriFactu).
/// </summary>
public sealed record FiscalSubmissionOutcome(
    FiscalSubmissionState State,
    string? Csv,
    string? Huella,
    string? ErrorCode,
    string? ErrorDescription)
{
    public bool IsAccepted => State is FiscalSubmissionState.Accepted or FiscalSubmissionState.AcceptedWithErrors;
}
