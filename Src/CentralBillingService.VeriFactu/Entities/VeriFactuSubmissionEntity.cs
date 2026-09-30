namespace CentralBillingService.VeriFactu.Entities;

/// <summary>
/// Authoritative per-invoice VeriFactu submission record: the stamped registro and the
/// outcome of reporting it to the AEAT. This — not the library's file blockchain — is the
/// source of truth we query, reprint and audit against.
/// </summary>
public sealed class VeriFactuSubmissionEntity
{
    public Guid Id { get; set; }
    public string BillingSource { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string IssuerNif { get; set; } = string.Empty;

    /// <summary>Position of this record in its (NIF, billing source) chain: records are submitted in this order.</summary>
    public long ChainSequence { get; set; }

    /// <summary>Pending / Sent / Accepted / AcceptedWithErrors / Rejected (see FiscalSubmissionState).</summary>
    public int State { get; set; }

    public string Huella { get; set; } = string.Empty;
    public string? PreviousHuella { get; set; }

    // Previous chain link + generation timestamp — persisted so Submit can rebuild the EXACT
    // RegistroAlta that produced Huella and register OUR huella at the AEAT (not a re-computed one).
    public string? PreviousNumSerie { get; set; }
    public string? PreviousFechaExpedicion { get; set; }
    public string? FechaHoraGenRegistro { get; set; }

    /// <summary>The serialized RegistroAlta/RegistroAnulacion pending submission.</summary>
    public string? RegistroXml { get; set; }

    public string? Csv { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? SentUtc { get; set; }
    public int RetryCount { get; set; }
}
