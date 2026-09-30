namespace CentralBillingService.VeriFactu.Entities;

/// <summary>
/// Authoritative AEAT huella chain, one row per (issuer NIF, billing source). Each billing source
/// is treated as an independent SIF (separate business with its own product line, identified by its
/// own NumeroInstalacion), so it keeps its own "encadenamiento" across all its series. Holds the last
/// registered invoice's identity + huella so the next record can reference it (RegistroAnterior).
/// Advanced under a pessimistic per-chain lock so it is strictly sequential under concurrency.
/// </summary>
public sealed class VeriFactuChainEntity
{
    public string Nif { get; set; } = string.Empty;
    public string BillingSource { get; set; } = string.Empty;
    public long Sequence { get; set; }

    // Previous record identity (RegistroAnterior) — null on the first record of the chain.
    public string? LastNumSerie { get; set; }
    public string? LastFechaExpedicion { get; set; }
    public string? LastHuella { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
