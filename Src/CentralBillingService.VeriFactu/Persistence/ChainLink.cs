namespace CentralBillingService.VeriFactu.Persistence;

/// <summary>Previous record in the AEAT chain for a NIF. First record when <see cref="Huella"/> is empty.</summary>
public sealed record ChainLink(string? NumSerie, string? FechaExpedicion, string? Huella)
{
    public bool IsFirst => string.IsNullOrEmpty(Huella);
}
