using CentralBillingService.Domain.ValueObjects; // ProductType
using VeriFactu.Xml.Factu;      // Impuesto
using VeriFactu.Xml.Factu.Alta; // TipoFactura
using VfInvoice = VeriFactu.Business.Invoice;
using VfTaxItem = VeriFactu.Business.TaxItem;
using DomainInvoice = CentralBillingService.Domain.Entities.Invoice;

namespace CentralBillingService.VeriFactu.Mapping;

/// <summary>
/// Maps a CBS domain <see cref="DomainInvoice"/> to the mdiago library's high-level
/// <c>VeriFactu.Business.Invoice</c>, from which the RegistroAlta, huella and QR are derived.
/// All fiscal inference (IVA vs IGIC, tax breakdown grouping) lives here, inside the adapter —
/// the domain stays authority-agnostic.
///
/// Tax breakdown (desglose) classification:
///  - Recipient in Spain ("ES") → subject and not exempt (CalificacionOperacion S1), taxed with
///    IVA or IGIC. IVA vs IGIC is inferred from the ISSUER's postal code (Canary Islands 35xxx
///    Las Palmas / 38xxx Santa Cruz de Tenerife → IGIC; mainland/Balearics → IVA), per the AEAT
///    rule that SIF obligations apply in the Canaries with IVA references read as IGIC.
///  - Recipient outside Spain → classified per line's <see cref="ProductType"/> (declared by the
///    billing source, so the system stays product-agnostic):
///      · Service → localized outside the Spanish VAT territory (TAI) → NOT SUBJECT (N2).
///      · Good → exported → EXEMPT: intra-EU delivery (CausaExencion E5) or export outside the EU
///        (CausaExencion E2).
///    Registered in EUR with base and no quota. ALL invoices are registered, including
///    0%/foreign-currency ones — the euro amounts already embed the exchange rate; the original
///    currency is internal-only and not part of the AEAT record.
///
/// NOTE (confirm with tax advisor / AEAT test env): reverse charge (ISP, S2) and B2C nuances
/// (e.g. EU digital services via OSS) are not modelled. Extend <see cref="BuildTaxItems"/> if needed.
/// </summary>
public sealed class InvoiceToVeriFactuMapper
{
    public VfInvoice Map(DomainInvoice invoice)
    {
        var vf = new VfInvoice(invoice.Number.Value, invoice.IssueDate.ToDateTime(TimeOnly.MinValue), invoice.Issuer.TaxId.Value)
        {
            SellerName = invoice.Issuer.LegalName,
            Text = BuildDescription(invoice),
            TaxItems = BuildTaxItems(invoice),
        };

        // Invoice type (F1/F2) and recipient identification (NIF / IDOtro / none).
        // NOTE (confirm with tax advisor): passports/official IDs for individuals are not modelled.
        VeriFactuRecipientIdentification.Apply(vf, invoice);

        return vf;
    }

    private static string BuildDescription(DomainInvoice invoice) =>
        !string.IsNullOrWhiteSpace(invoice.Notes) ? invoice.Notes!
        : invoice.Lines.Count > 0 ? invoice.Lines[0].Description
        : $"Factura {invoice.Number.Value}";

    private static List<VfTaxItem> BuildTaxItems(DomainInvoice invoice)
    {
        var tax = InferTax(invoice);

        if (!IsDomestic(invoice))
        {
            // Foreign customer: classify per line by product type (service → not subject;
            // good → exempt export). Group by the resulting classification; base in EUR, no quota.
            var euCustomer = IsEuCountry(invoice.Recipient.Address.CountryCode);
            return invoice.Lines
                .GroupBy(l => l.ProductType)
                .Select(g =>
                {
                    var baseEur = g.Sum(l => l.TaxableBaseEur.Amount);
                    return g.Key == ProductType.Service
                        ? new VfTaxItem // servicios: no sujeta por localización
                        {
                            TaxType = CalificacionOperacion.N2,
                            Tax = tax, TaxRate = 0m, TaxBase = baseEur, TaxAmount = 0m,
                        }
                        : new VfTaxItem // bienes: entrega exenta (intracomunitaria E5 / exportación E2)
                        {
                            TaxException = euCustomer ? CausaExencion.E5 : CausaExencion.E2,
                            Tax = tax, TaxRate = 0m, TaxBase = baseEur, TaxAmount = 0m,
                        };
                })
                .ToList();
        }

        // Domestic: subject and not exempt (S1), grouped by tax rate (one row per rate).
        return invoice.Lines
            .GroupBy(l => l.TaxRate.Percentage)
            .OrderBy(g => g.Key)
            .Select(g => new VfTaxItem
            {
                TaxType = CalificacionOperacion.S1,
                Tax = tax,
                TaxRate = g.Key,
                TaxBase = g.Sum(l => l.TaxableBaseEur.Amount),
                TaxAmount = g.Sum(l => l.TaxAmountEur.Amount),
            })
            .ToList();
    }

    /// <summary>True when the recipient is in Spain (subject to IVA/IGIC); false for foreign customers.</summary>
    private static bool IsDomestic(DomainInvoice invoice) => VeriFactuRecipientIdentification.IsDomestic(invoice);

    // Picks the exemption cause for exported goods: intra-EU (E5) vs export outside the EU (E2).
    private static bool IsEuCountry(string? code) => VeriFactuRecipientIdentification.IsEuCountry(code);

    /// <summary>IGIC for issuers in the Canary Islands (postal code 35xxx/38xxx), IVA otherwise.</summary>
    private static Impuesto InferTax(DomainInvoice invoice)
    {
        var pc = invoice.Issuer.Address.PostalCode ?? string.Empty;
        return pc.StartsWith("35", StringComparison.Ordinal) || pc.StartsWith("38", StringComparison.Ordinal)
            ? Impuesto.IGIC
            : Impuesto.IVA;
    }
}
