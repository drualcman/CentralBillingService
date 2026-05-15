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
