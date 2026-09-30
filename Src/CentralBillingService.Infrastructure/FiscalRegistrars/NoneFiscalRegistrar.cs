namespace CentralBillingService.Infrastructure.FiscalRegistrars;

/// <summary>
/// Default no-op fiscal registrar for billing sources that do not report to any
/// tax authority (RegistrarConfig.Type = "None"). Preserves the pre-VeriFactu behaviour:
/// no fiscal stamp, no submission. Always registered so the factory has a fallback.
/// </summary>
public sealed class NoneFiscalRegistrar : IFiscalRegistrar
{
    public const string TypeName = "None";

    public string RegistrarType => TypeName;

    public Task<FiscalStampResult> StampAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        Task.FromResult(FiscalStampResult.None);

    public Task<FiscalSubmissionOutcome> SubmitAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        Task.FromResult(new FiscalSubmissionOutcome(FiscalSubmissionState.Accepted, null, null, null, null));

    public Task<FiscalStampResult> StampRectificativeAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken cancellationToken = default) =>
        Task.FromResult(FiscalStampResult.None);

    public Task<FiscalSubmissionOutcome> SubmitRectificativeAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken cancellationToken = default) =>
        Task.FromResult(new FiscalSubmissionOutcome(FiscalSubmissionState.Accepted, null, null, null, null));

    public Task<IReadOnlyList<string>> GetPendingSubmissionsUpToAsync(string billingSource, string invoiceNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<string?> GetLatestStalledSubmissionAsync(string billingSource, DateTimeOffset stampedBefore, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
