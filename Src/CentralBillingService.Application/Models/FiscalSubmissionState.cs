namespace CentralBillingService.Application.Models;

/// <summary>
/// Lifecycle of an invoice's registration with an external fiscal authority.
/// </summary>
public enum FiscalSubmissionState
{
    /// <summary>Stamped locally, not yet sent to the authority.</summary>
    Pending = 0,

    /// <summary>Sent to the authority; final acceptance not yet confirmed.</summary>
    Sent = 1,

    /// <summary>Accepted by the authority.</summary>
    Accepted = 2,

    /// <summary>Accepted but the authority reported non-blocking errors/warnings.</summary>
    AcceptedWithErrors = 3,

    /// <summary>Rejected by the authority.</summary>
    Rejected = 4,
}
