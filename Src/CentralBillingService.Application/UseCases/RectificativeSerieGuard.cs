namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Rectificatives must be issued in their own specific series (RD 1619/2012 art. 6.1.a): a serie that the
/// billing source already uses for ordinary invoices cannot number rectificatives.
/// </summary>
public static class RectificativeSerieGuard
{
    public static async Task EnsureSerieIsNotUsedByOrdinaryInvoicesAsync(
        IInvoiceRepository repository,
        string billingSource,
        string serie,
        CancellationToken cancellationToken)
    {
        bool isOrdinaryInvoiceSerie = await repository.IsSerieUsedByOrdinaryInvoicesAsync(
            billingSource, serie.Trim().ToUpperInvariant(), cancellationToken);

        if (isOrdinaryInvoiceSerie)
            throw new DomainException(
                $"La serie '{serie}' ya se usa para facturas normales. " +
                "Las rectificativas deben emitirse en una serie propia (p.ej. R{serie}).");
    }
}
