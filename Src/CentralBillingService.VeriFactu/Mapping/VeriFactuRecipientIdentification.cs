using VeriFactu.Xml.Factu;      // IDType
using VeriFactu.Xml.Factu.Alta; // TipoFactura
using VfInvoice = VeriFactu.Business.Invoice;
using DomainInvoice = CentralBillingService.Domain.Entities.Invoice;

namespace CentralBillingService.VeriFactu.Mapping;

/// <summary>
/// Decides the invoice type and how the recipient is identified in the AEAT record.
/// The AEAT requires an identified recipient (Destinatarios) for a full invoice (F1); only a
/// simplified invoice (F2) may omit it. So:
///  - Recipient with tax id → F1. Domestic: Spanish NIF alone. Foreign: IDOtro with country and
///    NIF-IVA (EU) or "other supporting document" (non-EU).
///  - No tax id and total ≤ 400 € (art. 4 RD 1619/2012) → F2 simplified, no recipient block.
///  - No tax id and total &gt; 400 € → F1 with IDOtro "other supporting document", using the first
///    available recipient reference: ExternalId (the billing source's own customer id), phone, email.
/// </summary>
internal static class VeriFactuRecipientIdentification
{
    private const decimal SimplifiedInvoiceMaxTotalEur = 400m;
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
        string? recipientTaxId = invoice.Recipient.TaxId?.Value?.Trim();
        bool hasRecipientTaxId = !string.IsNullOrWhiteSpace(recipientTaxId) && invoice.Recipient.TaxId!.IsNotProvided == false;

        if (hasRecipientTaxId)
        {
            vf.InvoiceType = TipoFactura.F1;
            SetIdentifiedBuyer(vf, invoice, recipientTaxId!, GetForeignIdType(invoice));
        }
        else if (invoice.TotalEur.Amount <= SimplifiedInvoiceMaxTotalEur)
        {
            vf.InvoiceType = TipoFactura.F2;
            vf.BuyerID = null;
            vf.BuyerName = null;
        }
        else
        {
            vf.InvoiceType = TipoFactura.F1;
            SetIdentifiedBuyer(vf, invoice, GetOtherIdentifier(invoice), IDType.OTRO_DOC_PROBATORIO);
        }
    }

    public static bool IsDomestic(DomainInvoice invoice) =>
        string.Equals(invoice.Recipient.Address.CountryCode?.Trim(), SpainCountryCode, StringComparison.OrdinalIgnoreCase);

    public static bool IsEuCountry(string? countryCode) =>
        !string.IsNullOrWhiteSpace(countryCode) && EuCountries.Contains(countryCode.Trim());

    private static void SetIdentifiedBuyer(VfInvoice vf, DomainInvoice invoice, string buyerId, IDType foreignIdType)
    {
        vf.BuyerID = buyerId;
        vf.BuyerName = invoice.Recipient.LegalName;

        if (!IsDomestic(invoice))
        {
            vf.BuyerCountryID = invoice.Recipient.Address.CountryCode?.Trim();
            vf.BuyerIDType = foreignIdType;
        }
    }

    private static IDType GetForeignIdType(DomainInvoice invoice) =>
        IsEuCountry(invoice.Recipient.Address.CountryCode) ? IDType.NIF_IVA : IDType.OTRO_DOC_PROBATORIO;

    private static string GetOtherIdentifier(DomainInvoice invoice)
    {
        if (IsDomestic(invoice))
            throw new InvalidOperationException(
                $"Invoice '{invoice.Number.Value}' exceeds {SimplifiedInvoiceMaxTotalEur} € for a Spanish recipient without NIF: " +
                "a full invoice to a Spanish recipient requires the NIF.");

        string[] candidateIdentifiers = [invoice.Recipient.ExternalId ?? string.Empty, invoice.Recipient.Phone ?? string.Empty, invoice.Recipient.Email ?? string.Empty];
        string? identifier = candidateIdentifiers
            .Select(candidate => candidate.Trim())
            .FirstOrDefault(candidate => candidate.Length > 0 && candidate.Length <= MaxOtherIdentifierLength);

        if (identifier is null)
            throw new InvalidOperationException(
                $"Invoice '{invoice.Number.Value}' exceeds {SimplifiedInvoiceMaxTotalEur} € and the foreign recipient has no tax id, " +
                $"external id, phone or email of at most {MaxOtherIdentifierLength} characters to identify it at the AEAT.");

        return identifier;
    }
}
