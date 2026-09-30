namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Selects the <see cref="IFiscalRegistrar"/> for a billing source based on
/// RegistrarConfig.Type. Mirrors <see cref="IInvoiceNumberProviderFactory"/>.
/// Sources with Type = "None" get a no-op registrar (no fiscal reporting).
/// </summary>
public interface IFiscalRegistrarFactory
{
    IFiscalRegistrar GetFor(BillingSourceConfig config);
}
