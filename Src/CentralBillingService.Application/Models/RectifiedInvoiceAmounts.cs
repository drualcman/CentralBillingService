namespace CentralBillingService.Application.Models;

/// <summary>
/// Amounts (EUR) of the document being rectified. Fiscal authorities need them for a rectification by
/// substitution (e.g. VeriFactu ImporteRectificacion), since the rectificative then carries the full
/// corrected amounts, which no longer tell what the rectified document declared.
/// </summary>
public sealed record RectifiedInvoiceAmounts(decimal TaxableBaseEur, decimal TaxAmountEur)
{
    public decimal TotalEur => TaxableBaseEur + TaxAmountEur;

    public static RectifiedInvoiceAmounts From(Invoice invoice) =>
        new RectifiedInvoiceAmounts(invoice.TaxableBaseEur.Amount, invoice.TotalTaxAmountEur.Amount);

    public static RectifiedInvoiceAmounts From(RectificativeInvoice rectificative) =>
        new RectifiedInvoiceAmounts(rectificative.TaxableBaseEur.Amount, rectificative.TotalTaxAmountEur.Amount);
}
