namespace CentralBillingService.Infrastructure.Persistence;

/// <summary>
/// Implements the application's IInvoiceRepository port by delegating
/// reads to IInvoiceReadContext and writes to IInvoiceWriteContext.
/// </summary>
public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly IInvoiceReadContext _read;
    private readonly IInvoiceWriteContext _write;

    public InvoiceRepository(IInvoiceReadContext read, IInvoiceWriteContext write)
    {
        _read = read;
        _write = write;
    }

    // ── Reads (delegated to IInvoiceReadContext) ───────────────────────────

    public Task<Invoice?> FindByIdAsync(
        string billingSource, Guid id, CancellationToken cancellationToken = default) =>
        _read.FindByIdAsync(billingSource, id, cancellationToken);

    public Task<Invoice?> FindByNumberAsync(
        string billingSource, string invoiceNumber, CancellationToken cancellationToken = default) =>
        _read.FindByNumberAsync(billingSource, invoiceNumber, cancellationToken);

    public Task<Invoice?> FindByPaymentReferenceAsync(
        string billingSource, string paymentReference, CancellationToken cancellationToken = default) =>
        _read.FindByPaymentReferenceAsync(billingSource, paymentReference, cancellationToken);

    public Task<string?> GetLastHashAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default) =>
        _read.GetLastHashAsync(billingSource, serie, year, cancellationToken);

    public Task<InvoicePagedResult> ListAsync(
        InvoiceFilter filter,
        CancellationToken cancellationToken = default) =>
        _read.ListAsync(filter, cancellationToken);

    public Task<RectificativeInvoice?> FindRectificativeByNumberAsync(
        string billingSource,
        string invoiceNumber,
        CancellationToken cancellationToken = default) =>
        _read.FindRectificativeByNumberAsync(billingSource, invoiceNumber, cancellationToken);

    public Task<IReadOnlyList<InvoiceSummaryDataPoint>> GetSummaryDataAsync(
        string? billingSource,
        CancellationToken cancellationToken = default) =>
        _read.GetSummaryDataAsync(billingSource, cancellationToken);

    // ── Writes (delegated to IInvoiceWriteContext) ─────────────────────────

    public Task SaveAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        _write.SaveAsync(invoice, cancellationToken);

    public Task<Invoice> CreateAtomicAsync(
        string billingSource,
        string serie,
        int year,
        Func<int, string?, CancellationToken, Task<Invoice>> buildInvoice,
        CancellationToken cancellationToken = default) =>
        _write.CreateAtomicAsync(billingSource, serie, year, buildInvoice, cancellationToken);

    public Task SaveRectificativeAsync(
        RectificativeInvoice rectificative,
        Invoice updatedOriginal,
        CancellationToken cancellationToken = default) =>
        _write.SaveRectificativeAsync(rectificative, updatedOriginal, cancellationToken);

    public Task SaveRectificativeFromRectificativeAsync(
        RectificativeInvoice rectificative,
        RectificativeInvoice updatedOriginal,
        CancellationToken cancellationToken = default) =>
        _write.SaveRectificativeFromRectificativeAsync(rectificative, updatedOriginal, cancellationToken);

}
