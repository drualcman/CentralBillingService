namespace CentralBillingService.VeriFactu;

/// <summary>
/// Builds a <see cref="VeriFactuFiscalRegistrar"/> bound to a billing source's own VeriFactu
/// settings (certificate, environment, endpoint), read from that source's <c>Registrar.Settings</c>.
/// Registered once per process; the factory calls <see cref="Build"/> per source. The scoped store
/// is shared (it targets the CbsDb chain/submission tables regardless of source).
/// </summary>
public sealed class VeriFactuFiscalRegistrarBuilder : IFiscalRegistrarBuilder
{
    private readonly IVeriFactuStore _store;
    private readonly InvoiceToVeriFactuMapper _mapper;
    private readonly ILogger<VeriFactuFiscalRegistrar> _logger;

    public VeriFactuFiscalRegistrarBuilder(
        IVeriFactuStore store,
        InvoiceToVeriFactuMapper mapper,
        ILogger<VeriFactuFiscalRegistrar> logger)
    {
        _store = store;
        _mapper = mapper;
        _logger = logger;
    }

    public string RegistrarType => VeriFactuFiscalRegistrar.TypeName;

    public IFiscalRegistrar Build(BillingSourceConfig config)
    {
        // The SIF producer identity defaults to this source's issuer (own-use software); the
        // settings bag can still override it when a third party developed the SIF.
        var options = VeriFactuOptions.FromSettings(
            config.Registrar?.Settings,
            issuerNif: config.Issuer?.TaxIdValue ?? string.Empty,
            issuerLegalName: config.Issuer?.LegalName ?? string.Empty,
            billingSource: config.BillingSource);
        return new VeriFactuFiscalRegistrar(_store, _mapper, options, _logger);
    }
}
