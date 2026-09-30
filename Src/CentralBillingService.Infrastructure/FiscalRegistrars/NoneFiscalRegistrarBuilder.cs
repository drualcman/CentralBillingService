namespace CentralBillingService.Infrastructure.FiscalRegistrars;

/// <summary>
/// Builder for the default no-op registrar. Always registered so the factory has a fallback
/// for sources with Registrar.Type = "None" (or unset). Stateless — returns a shared instance.
/// </summary>
public sealed class NoneFiscalRegistrarBuilder : IFiscalRegistrarBuilder
{
    public const string TypeName = NoneFiscalRegistrar.TypeName;

    private static readonly NoneFiscalRegistrar Instance = new();

    public string RegistrarType => TypeName;

    public IFiscalRegistrar Build(BillingSourceConfig config) => Instance;
}
