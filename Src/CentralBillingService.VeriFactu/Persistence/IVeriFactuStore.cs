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

    /// <summary>
    /// Runs <paramref name="action"/> holding an exclusive, cross-process lock on the (NIF, billing source)
    /// chain, so two workers never submit the same chain concurrently (no duplicate sends, no lost CSV).
    /// </summary>
    Task<T> RunExclusiveOnChainAsync<T>(
        string nif, string billingSource, Func<Task<T>> action, CancellationToken cancellationToken = default);

    /// <summary>Pending invoice numbers of the chain with sequence ≤ <paramref name="chainSequence"/>, in chain order.</summary>
    Task<IReadOnlyList<string>> GetPendingInChainUpToAsync(
        string nif, string billingSource, long chainSequence, CancellationToken cancellationToken = default);

    /// <summary>Highest-sequence pending invoice number of the billing source stamped before the cutoff.</summary>
    Task<string?> GetLatestPendingStampedBeforeAsync(
        string billingSource, DateTimeOffset stampedBefore, CancellationToken cancellationToken = default);

    Task UpdateSubmissionAsync(VeriFactuSubmissionEntity entity, CancellationToken cancellationToken = default);
}
