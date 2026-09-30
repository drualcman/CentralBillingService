using VeriFactu.Xml.Factu;      // IDType
using VeriFactu.Xml.Factu.Alta; // TipoFactura
using VfInvoice = VeriFactu.Business.Invoice;
using DomainInvoice = CentralBillingService.Domain.Entities.Invoice;

namespace CentralBillingService.VeriFactu.Mapping;

/// <summary>
/// Decides the record type and how the recipient is identified in the AEAT record.
/// The AEAT requires an identified recipient (Destinatarios) for a full invoice (F1) or its
/// rectificative (R1); only a simplified invoice (F2) or its rectificative (R5) may omit it. So:
///  - Recipient with tax id → F1 / R1. Domestic: Spanish NIF alone. Foreign: IDOtro with country and
///    NIF-IVA (EU) or "other supporting document" (non-EU).
///  - Simplified (no tax id and invoice total ≤ 400 €, art. 4 RD 1619/2012) → F2; a rectificative of
///    a simplified invoice → R5. No recipient block.
///  - Otherwise, no tax id → F1 / R1 with IDOtro "other supporting document", using the first
///    available recipient reference: ExternalId (the billing source's own customer id), phone, email.
/// </summary>
internal static class VeriFactuRecipientIdentification
{
    public const decimal SimplifiedInvoiceMaxTotalEur = 400m;
    private const int MaxOtherIdentifierLength = 20; // AEAT IDOtro/ID: TextMax20
    private const string SpainCountryCode = "ES";

    // EU member states (ISO 3166-1 alpha-2), excluding ES (handled as domestic).
    private static readonly HashSet<string> EuCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","HU","IE","IT",
        "LV","LT","LU","MT","NL","PL","PT","RO","SK","SI","SE",
    };

    public static void Apply(VfInvoice vf, DomainInvoice invoice)
    {
        bool isSimplified = !HasTaxId(invoice.Recipient) && invoice.TotalEur.Amount <= SimplifiedInvoiceMaxTotalEur;
        ApplyCore(vf, invoice.Recipient, invoice.Number.Value, isSimplified, TipoFactura.F1, TipoFactura.F2);
    }

    public static void ApplyToRectificative(VfInvoice vf, RectificativeInvoice rectificative, bool rectifiesSimplifiedInvoice)
    {
        bool isSimplified = !HasTaxId(rectificative.Recipient) && rectifiesSimplifiedInvoice;
        ApplyCore(vf, rectificative.Recipient, rectificative.Number.Value, isSimplified, TipoFactura.R1, TipoFactura.R5);
    }

    public static bool HasTaxId(BillingParty recipient) =>
        !string.IsNullOrWhiteSpace(recipient.TaxId?.Value) && recipient.TaxId!.IsNotProvided == false;

    public static bool IsDomestic(BillingParty recipient) =>
        string.Equals(recipient.Address.CountryCode?.Trim(), SpainCountryCode, StringComparison.OrdinalIgnoreCase);

    public static bool IsDomestic(DomainInvoice invoice) => IsDomestic(invoice.Recipient);

    public static bool IsEuCountry(string? countryCode) =>
        !string.IsNullOrWhiteSpace(countryCode) && EuCountries.Contains(countryCode.Trim());

    private static void ApplyCore(
        VfInvoice vf, BillingParty recipient, string documentNumber, bool isSimplified,
        TipoFactura fullType, TipoFactura simplifiedType)
    {
        if (HasTaxId(recipient))
        {
            vf.InvoiceType = fullType;
            SetIdentifiedBuyer(vf, recipient, recipient.TaxId.Value.Trim(), GetForeignIdType(recipient));
        }
        else if (isSimplified)
        {
            vf.InvoiceType = simplifiedType;
            vf.BuyerID = null;
            vf.BuyerName = null;
        }
        else
        {
            vf.InvoiceType = fullType;
            SetIdentifiedBuyer(vf, recipient, GetOtherIdentifier(recipient, documentNumber), IDType.OTRO_DOC_PROBATORIO);
        }
    }

    private static void SetIdentifiedBuyer(VfInvoice vf, BillingParty recipient, string buyerId, IDType foreignIdType)
    {
        vf.BuyerID = buyerId;
        vf.BuyerName = recipient.LegalName;

        if (!IsDomestic(recipient))
        {
            vf.BuyerCountryID = recipient.Address.CountryCode?.Trim();
            vf.BuyerIDType = foreignIdType;
        }
    }

    private static IDType GetForeignIdType(BillingParty recipient) =>
        IsEuCountry(recipient.Address.CountryCode) ? IDType.NIF_IVA : IDType.OTRO_DOC_PROBATORIO;

    private static string GetOtherIdentifier(BillingParty recipient, string documentNumber)
    {
        if (IsDomestic(recipient))
            throw new InvalidOperationException(
                $"'{documentNumber}' is not a simplified invoice and its Spanish recipient has no NIF: " +
                "a full invoice to a Spanish recipient requires the NIF.");

        string[] candidateIdentifiers = [recipient.ExternalId ?? string.Empty, recipient.Phone ?? string.Empty, recipient.Email ?? string.Empty];
        string? identifier = candidateIdentifiers
            .Select(candidate => candidate.Trim())
            .FirstOrDefault(candidate => candidate.Length > 0 && candidate.Length <= MaxOtherIdentifierLength);

        if (identifier is null)
            throw new InvalidOperationException(
                $"'{documentNumber}' is not a simplified invoice and the foreign recipient has no tax id, " +
                $"external id, phone or email of at most {MaxOtherIdentifierLength} characters to identify it at the AEAT.");

        return identifier;
    }
}
