namespace CentralBillingService.Infrastructure.FiscalRegistrars;

/// <summary>
/// Resolves the <see cref="IFiscalRegistrar"/> for a billing source by RegistrarConfig.Type,
/// then asks the matching <see cref="IFiscalRegistrarBuilder"/> to build one bound to that
/// source's settings (its own certificate/endpoint). Unknown non-"None" types fail fast —
/// a source asking for a registrar that was never wired is a configuration error.
/// </summary>
public sealed class FiscalRegistrarFactory : IFiscalRegistrarFactory
{
    private readonly IReadOnlyDictionary<string, IFiscalRegistrarBuilder> _builders;

    public FiscalRegistrarFactory(IEnumerable<IFiscalRegistrarBuilder> builders) =>
        _builders = builders.ToDictionary(b => b.RegistrarType, StringComparer.OrdinalIgnoreCase);

    public IFiscalRegistrar GetFor(BillingSourceConfig config)
    {
        var type = string.IsNullOrWhiteSpace(config.Registrar?.Type)
            ? NoneFiscalRegistrarBuilder.TypeName
            : config.Registrar.Type;

        if (_builders.TryGetValue(type, out var builder))
            return builder.Build(config);

        throw new InvalidOperationException(
            $"No fiscal registrar registered for type '{type}'. " +
            $"Register an {nameof(IFiscalRegistrarBuilder)} whose {nameof(IFiscalRegistrarBuilder.RegistrarType)} = '{type}' " +
            $"(e.g. AddVeriFactuRegistrar()), or set the source's Registrar.Type to 'None'.");
    }
}
