namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Economic nature of what an invoice line bills: a service or a physical good.
///
/// Generic and authority-agnostic — it belongs to the invoice domain, not to any tax system.
/// Each concrete billing source declares it per product/line. Fiscal registrars map it to their
/// own rules; e.g. for the Spanish AEAT it drives how FOREIGN sales are classified in VeriFactu:
/// a service localized abroad is "not subject" (localization), whereas a good shipped abroad is an
/// "exempt export". Defaults to <see cref="Service"/> for back-compatibility with existing data.
/// </summary>
public enum ProductType
{
    Service = 0,
    Good = 1,
}
