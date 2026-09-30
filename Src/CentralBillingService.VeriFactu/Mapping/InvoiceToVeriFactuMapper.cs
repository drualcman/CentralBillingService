using VeriFactu.Xml.Factu.Alta; // TipoRectificativa
using VfInvoice = VeriFactu.Business.Invoice;
using VfRectificationItem = VeriFactu.Business.RectificationItem;
using DomainInvoice = CentralBillingService.Domain.Entities.Invoice;

namespace CentralBillingService.VeriFactu.Mapping;

/// <summary>
/// Maps CBS domain invoices and rectificatives to the mdiago library's high-level
/// <c>VeriFactu.Business.Invoice</c>, from which the RegistroAlta, huella and QR are derived.
/// All fiscal inference lives inside the adapter — the domain stays authority-agnostic:
/// the tax breakdown in <see cref="VeriFactuTaxBreakdownBuilder"/>, the record type and recipient
/// identification in <see cref="VeriFactuRecipientIdentification"/>.
///
/// Rectificatives (R1, or R5 when rectifying a simplified invoice) reference the rectified invoice:
///  - Substitution → AEAT "S": the rectificative carries the full corrected amounts and
///    ImporteRectificacion carries the amounts the rectified invoice declared.
///  - Difference → AEAT "I": the rectificative carries only the (signed) difference.
/// </summary>
public sealed class InvoiceToVeriFactuMapper
{
    public VfInvoice Map(DomainInvoice invoice)
    {
        VfInvoice vf = new VfInvoice(invoice.Number.Value, invoice.IssueDate.ToDateTime(TimeOnly.MinValue), invoice.Issuer.TaxId.Value)
        {
            SellerName = invoice.Issuer.LegalName,
            Text = BuildDescription(invoice),
            TaxItems = VeriFactuTaxBreakdownBuilder.Build(invoice.Lines, invoice.Issuer, invoice.Recipient),
        };

        // Invoice type (F1/F2) and recipient identification (NIF / IDOtro / none).
        // NOTE (confirm with tax advisor): passports/official IDs for individuals are not modelled.
        VeriFactuRecipientIdentification.Apply(vf, invoice);

        return vf;
    }

    public VfInvoice MapRectificative(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, bool rectifiesSimplifiedInvoice)
    {
        bool isSubstitution = rectificative.RectificationType == RectificationType.Substitution;

        VfInvoice vf = new VfInvoice(rectificative.Number.Value, rectificative.IssueDate.ToDateTime(TimeOnly.MinValue), rectificative.Issuer.TaxId.Value)
        {
            SellerName = rectificative.Issuer.LegalName,
            Text = BuildDescription(rectificative),
            TaxItems = VeriFactuTaxBreakdownBuilder.Build(rectificative.Lines, rectificative.Issuer, rectificative.Recipient),
            RectificationType = isSubstitution ? TipoRectificativa.S : TipoRectificativa.I,
            RectificationItems = new List<VfRectificationItem>
            {
                new VfRectificationItem
                {
                    InvoiceID = rectificative.OriginalInvoiceNumber.Value,
                    InvoiceDate = rectificative.OriginalIssueDate.ToDateTime(TimeOnly.MinValue),
                }
            },
        };

        if (isSubstitution)
        {
            vf.RectificationTaxBase = VeriFactuTaxBreakdownBuilder.ToCents(rectifiedAmounts.TaxableBaseEur);
            vf.RectificationTaxAmount = VeriFactuTaxBreakdownBuilder.ToCents(rectifiedAmounts.TaxAmountEur);
        }

        // Record type (R1/R5) and recipient identification, same rules as the rectified invoice.
        VeriFactuRecipientIdentification.ApplyToRectificative(vf, rectificative, rectifiesSimplifiedInvoice);

        return vf;
    }

    /// <summary>
    /// Whether a rectificative rectifies a simplified invoice (→ R5), when the rectified invoice's own
    /// record type is unknown (e.g. it was issued before VeriFactu was enabled): the invoice rule applied
    /// to the amounts the rectified invoice declared.
    /// </summary>
    public static bool IsLikelyRectifyingSimplifiedInvoice(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts) =>
        !VeriFactuRecipientIdentification.HasTaxId(rectificative.Recipient)
        && Math.Abs(rectifiedAmounts.TotalEur) <= VeriFactuRecipientIdentification.SimplifiedInvoiceMaxTotalEur;

    private static string BuildDescription(DomainInvoice invoice) =>
        !string.IsNullOrWhiteSpace(invoice.Notes) ? invoice.Notes!
        : invoice.Lines.Count > 0 ? invoice.Lines[0].Description
        : $"Factura {invoice.Number.Value}";

    private static string BuildDescription(RectificativeInvoice rectificative) =>
        $"Rectificación de {rectificative.OriginalInvoiceNumber.Value}: {rectificative.RectificationReason}";
}
