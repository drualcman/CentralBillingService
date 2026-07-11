namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Port for reserving the next invoice sequence number.
/// Defined in Application so use cases can depend on it
/// without knowing the concrete reservation strategy.
///
/// Implementations:
///   - DatabaseInvoiceNumberProvider: reserves from the local SQL database (Spain / VeriFactu)
///   - Any API-based implementation: calls a government API that issues the number (e.g. Mexico SAT)
/// </summary>
public interface IInvoiceNumberProvider
{
    /// <summary>
    /// True when the number authority is the local billing database (e.g. Spain / VeriFactu).
    /// In that case the number reservation and the invoice persistence can — and must — happen
    /// in a single database transaction (see IInvoiceRepository.CreateAtomicAsync), so a
    /// cancellation or failure rolls back cleanly and never leaves a reserved number without
    /// its invoice (a gap in the correlative numbering).
    ///
    /// False when the number is issued by an external authority (e.g. Mexico SAT): the number
    /// is obtained first via <see cref="ReserveNextNumberAsync"/> and the invoice is persisted
    /// afterwards, because the local database cannot roll back a number the authority already issued.
    /// </summary>
    bool ReservesFromLocalDatabase { get; }

    /// <summary>
    /// Atomically reserves and returns the next sequence number for the given
    /// BillingSource + Serie + Year combination.
    ///
    /// The returned number must be unique and durable — once reserved,
    /// the same number must not be issued to any other invoice.
    /// </summary>
    Task<int> ReserveNextNumberAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default);
}
