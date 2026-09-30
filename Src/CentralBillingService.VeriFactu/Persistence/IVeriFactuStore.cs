namespace CentralBillingService.VeriFactu.Persistence;

public interface IVeriFactuStore
{
    /// <summary>
    /// Atomically advances the (NIF, billing source) huella chain and records a Pending submission, invoking
    /// <paramref name="compute"/> with the previous link so the caller can set Encadenamiento and
    /// compute the official huella. Idempotent: if a submission already exists for
    /// (billingSource, invoiceNumber), returns its huella without advancing the chain. Returns the huella.
    /// </summary>
    Task<string> StampAsync(
        string nif, string billingSource, string invoiceNumber,
        string newNumSerie, string newFechaExpedicion,
        Func<ChainLink, StampComputation> compute,
        CancellationToken cancellationToken = default);

    Task<VeriFactuSubmissionEntity?> GetSubmissionAsync(
        string billingSource, string invoiceNumber, CancellationToken cancellationToken = default);

    Task UpdateSubmissionAsync(VeriFactuSubmissionEntity entity, CancellationToken cancellationToken = default);
}
