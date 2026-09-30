namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Payload sent to the fiscal-submission queue. The background worker loads the invoice
/// and hands it to the billing source's <see cref="Interfaces.IFiscalRegistrar"/> to report
/// it to the tax authority (e.g. AEAT VeriFactu). Authority-agnostic by design.
/// </summary>
public sealed record SubmitFiscalRecordCommand(
    string InvoiceNumber,
    string BillingSource);
