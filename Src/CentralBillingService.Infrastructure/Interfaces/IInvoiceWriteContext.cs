namespace CentralBillingService.Infrastructure.Interfaces;

/// <summary>
/// Write-side context: all commands the repository needs from the storage layer.
/// Implemented in the persistence project (e.g. Persistence.SqlServer, Persistence.CosmosDb).
///
/// Every method here mutates state. Implementations must guarantee:
/// - Atomicity: all changes in a single operation succeed or fail together
/// - Immutability: issued invoices are never overwritten, only appended
/// - Auditability: a full audit log is maintained alongside each record
/// </summary>
public interface IInvoiceWriteContext
{
    /// <summary>
    /// Atomically reserves and returns the next sequence number for
    /// BillingSource + Serie + Year.
    ///
    /// The counter is durable and persisted alongside invoices.
    /// Implementations must guarantee no two concurrent callers
    /// receive the same number (e.g. via SELECT FOR UPDATE, SEQUENCE,
    /// Redis INCR, or Cosmos stored procedure).
    /// </summary>
    Task<int> ReserveNextNumberAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves the next sequence number AND persists the resulting invoice in a single
    /// database transaction, so the two can never diverge.
    ///
    /// The implementation must, within one transaction:
    ///   1. Take a lock on the BillingSource+Serie+Year sequence row (pessimistic, per-key,
    ///      so different billing sources / series do not block each other).
    ///   2. Compute the next number and read the previous chain hash from that row.
    ///   3. Invoke <paramref name="buildInvoice"/> with (reservedNumber, previousHash) to build
    ///      the fully-formed, hashed invoice (this is where the domain runs).
    ///   4. Advance the sequence (LastNumber + LastHash) and insert the invoice.
    ///   5. Commit.
    ///
    /// If anything throws or is cancelled before commit, the transaction rolls back and NO
    /// number is consumed — eliminating gaps in the correlative numbering required by VeriFactu.
    /// <paramref name="buildInvoice"/> may be invoked more than once if the transaction is
    /// retried after a transient database failure, so it must be free of external side effects
    /// that cannot be repeated.
    /// </summary>
    Task<Invoice> CreateAtomicAsync(
        string billingSource,
        string serie,
        int year,
        Func<int, string?, CancellationToken, Task<Invoice>> buildInvoice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a newly created invoice.
    ///
    /// Preconditions the implementation must enforce:
    /// - Invoice.Status must be Issued
    /// - No existing record with the same Id or InvoiceNumber
    ///
    /// The implementation is also responsible for atomically updating
    /// the sequence counter and last hash for BillingSource+Serie+Year,
    /// so that concurrent requests cannot produce duplicate numbers
    /// or break the hash chain.
    /// </summary>
    Task SaveAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a rectificative invoice and updates the original invoice status
    /// in a single atomic operation.
    ///
    /// Both records must be written in the same transaction:
    /// - Insert the rectificative (new record)
    /// - Update the original's Status → Rectified and set RectifiedBy
    ///
    /// If either write fails, both must be rolled back.
    /// </summary>
    Task SaveRectificativeAsync(
        RectificativeInvoice rectificative,
        Invoice updatedOriginal,
        CancellationToken cancellationToken = default);

    Task SaveRectificativeFromRectificativeAsync(
        RectificativeInvoice rectificative,
        RectificativeInvoice updatedOriginal,
        CancellationToken cancellationToken = default);

}
