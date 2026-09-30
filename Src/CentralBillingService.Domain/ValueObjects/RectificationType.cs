namespace CentralBillingService.Domain.ValueObjects;

public enum RectificationType
{
    /// <summary>
    /// The rectificative carries the full corrected invoice (every line as it should read) and
    /// replaces the original. AEAT VeriFactu type "S".
    /// </summary>
    Substitution,

    /// <summary>
    /// The rectificative carries only the signed difference: e.g. a negative line cancelling all or part
    /// of an original line, plus new positive lines. AEAT VeriFactu type "I".
    /// </summary>
    Difference
}
