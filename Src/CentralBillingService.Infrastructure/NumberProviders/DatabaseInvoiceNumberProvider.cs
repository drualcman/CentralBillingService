namespace CentralBillingService.Infrastructure.NumberProviders;

/// <summary>
/// Reserves invoice sequence numbers from the local billing database.
/// Use this when the number authority is the local system (e.g. Spain / VeriFactu).
///
/// For government-issued numbers (e.g. Mexico SAT), implement IInvoiceNumberProvider
/// against the relevant external API instead.
/// </summary>
public sealed class DatabaseInvoiceNumberProvider : IInvoiceNumberProvider
{
    private readonly IInvoiceWriteContext _write;

    public DatabaseInvoiceNumberProvider(IInvoiceWriteContext write)
    {
        _write = write;
    }

    public Task<int> ReserveNextNumberAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default) =>
        _write.ReserveNextNumberAsync(billingSource, serie, year, cancellationToken);
}
