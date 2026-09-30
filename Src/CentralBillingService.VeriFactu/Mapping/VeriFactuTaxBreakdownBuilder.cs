using CentralBillingService.Domain.ValueObjects; // ProductType
using VeriFactu.Xml.Factu;      // Impuesto
using VeriFactu.Xml.Factu.Alta; // CalificacionOperacion, CausaExencion
using VfTaxItem = VeriFactu.Business.TaxItem;

namespace CentralBillingService.VeriFactu.Mapping;

/// <summary>
/// Builds the AEAT tax breakdown (desglose) for an invoice or a rectificative from its lines.
///  - Recipient in Spain ("ES") → subject and not exempt (S1), one row per tax rate, taxed with IVA
///    or IGIC. IGIC is inferred from the ISSUER's postal code (Canary Islands 35xxx / 38xxx).
///  - Recipient outside Spain → per line's <see cref="ProductType"/>: Service → not subject by
///    localization (N2); Good → exempt export, intra-EU (E5) or outside the EU (E2). Base in EUR, no quota.
/// Amounts keep their sign (a rectificative by differences may be negative) and a two-decimal scale.
/// NOTE (confirm with tax advisor): reverse charge (ISP, S2) and B2C OSS nuances are not modelled.
/// </summary>
internal static class VeriFactuTaxBreakdownBuilder
{
    public static List<VfTaxItem> Build(IReadOnlyList<InvoiceLine> lines, BillingParty issuer, BillingParty recipient)
    {
        Impuesto tax = InferTax(issuer);
        List<VfTaxItem> taxItems = VeriFactuRecipientIdentification.IsDomestic(recipient)
            ? BuildDomestic(lines, tax)
            : BuildForeign(lines, tax, VeriFactuRecipientIdentification.IsEuCountry(recipient.Address.CountryCode));

        return taxItems;
    }

    /// <summary>
    /// Rounds to cents AND forces a two-decimal scale. The library writes amounts with the decimal's
    /// own scale (15m → "15"), but the AEAT computes the huella with "15.00"; without this, round
    /// amounts (e.g. 0 % tax) produce a huella the AEAT flags as incorrect (error 2000).
    /// </summary>
    public static decimal ToCents(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero) + 0.00m;

    private static List<VfTaxItem> BuildDomestic(IReadOnlyList<InvoiceLine> lines, Impuesto tax) =>
        lines
            .GroupBy(line => line.TaxRate.Percentage)
            .OrderBy(group => group.Key)
            .Select(group => new VfTaxItem
            {
                TaxType = CalificacionOperacion.S1,
                Tax = tax,
                TaxRate = group.Key,
                TaxBase = ToCents(group.Sum(line => line.TaxableBaseEur.Amount)),
                TaxAmount = ToCents(group.Sum(line => line.TaxAmountEur.Amount)),
            })
            .ToList();

    private static List<VfTaxItem> BuildForeign(IReadOnlyList<InvoiceLine> lines, Impuesto tax, bool isEuCustomer) =>
        lines
            .GroupBy(line => line.ProductType)
            .Select(group =>
            {
                decimal baseEur = ToCents(group.Sum(line => line.TaxableBaseEur.Amount));
                VfTaxItem taxItem = group.Key == ProductType.Service
                    ? new VfTaxItem { TaxType = CalificacionOperacion.N2 }
                    : new VfTaxItem { TaxException = isEuCustomer ? CausaExencion.E5 : CausaExencion.E2 };
                taxItem.Tax = tax;
                taxItem.TaxRate = 0m;
                taxItem.TaxBase = baseEur;
                taxItem.TaxAmount = ToCents(0m);
                return taxItem;
            })
            .ToList();

    /// <summary>IGIC for issuers in the Canary Islands (postal code 35xxx/38xxx), IVA otherwise.</summary>
    private static Impuesto InferTax(BillingParty issuer)
    {
        string postalCode = issuer.Address.PostalCode ?? string.Empty;
        bool isCanaryIslands = postalCode.StartsWith("35", StringComparison.Ordinal) || postalCode.StartsWith("38", StringComparison.Ordinal);
        return isCanaryIslands ? Impuesto.IGIC : Impuesto.IVA;
    }
}
