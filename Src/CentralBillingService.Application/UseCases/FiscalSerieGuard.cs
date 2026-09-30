namespace CentralBillingService.Application.UseCases;

/// <summary>
/// A serie belongs to one (billing source, issuer NIF): the same billing source keeps using it freely
/// and sources with a different NIF may reuse its name, but another billing source of the SAME NIF may
/// not. The tax authority identifies an invoice by NIF + number (+ date) regardless of the billing
/// system, so a shared serie would yield duplicate invoice numbers that the authority rejects.
/// Enforced only for billing sources reporting to a fiscal registrar.
/// </summary>
public static class FiscalSerieGuard
{
    public static async Task EnsureSerieIsNotSharedAsync(
        IInvoiceRepository repository,
        BillingSourceConfig config,
        string serie,
        int year,
        CancellationToken cancellationToken)
    {
        bool reportsToFiscalAuthority = config.Registrar is not null && !config.Registrar.IsNone;
        string issuerTaxId = config.Issuer?.TaxIdValue ?? string.Empty;

        if (reportsToFiscalAuthority && !string.IsNullOrWhiteSpace(issuerTaxId))
        {
            bool isShared = await repository.IsSerieUsedByAnotherBillingSourceAsync(
                issuerTaxId, serie, year, config.BillingSource, cancellationToken);

            if (isShared)
                throw new DomainException(
                    $"La serie '{serie}' ya la usa otra fuente de facturación con el mismo NIF ({issuerTaxId}) en {year}. " +
                    "Hacienda identifica la factura por NIF y número: elige una serie propia para esta fuente.");
        }
    }
}
