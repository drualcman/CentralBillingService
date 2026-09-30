using CentralBillingService.Infrastructure.FiscalRegistrars;

namespace CentralBillingService.Tests.Unit.Infrastructure.FiscalRegistrars;

public class FiscalRegistrarFactoryTests
{
    private static BillingSourceConfig ConfigWith(string? registrarType) => new()
    {
        BillingSource = "web-test",
        Secret = "s",
        Registrar = registrarType is null ? new RegistrarConfig() : new RegistrarConfig { Type = registrarType }
    };

    private sealed class FakeVeriFactuRegistrar : IFiscalRegistrar
    {
        public string RegistrarType => "VeriFactu";
        public Task<FiscalStampResult> StampAsync(Invoice invoice, CancellationToken ct = default) =>
            Task.FromResult(new FiscalStampResult("HUELLA", "https://aeat/qr"));
        public Task<FiscalSubmissionOutcome> SubmitAsync(Invoice invoice, CancellationToken ct = default) =>
            Task.FromResult(new FiscalSubmissionOutcome(FiscalSubmissionState.Accepted, "CSV", "HUELLA", null, null));
    }

    /// <summary>Records the config it was built with, so tests can assert per-source binding.</summary>
    private sealed class FakeVeriFactuBuilder : IFiscalRegistrarBuilder
    {
        public BillingSourceConfig? LastConfig { get; private set; }
        public string RegistrarType => "VeriFactu";
        public IFiscalRegistrar Build(BillingSourceConfig config)
        {
            LastConfig = config;
            return new FakeVeriFactuRegistrar();
        }
    }

    [Fact]
    public void Defaults_to_None_registrar_when_type_absent()
    {
        var factory = new FiscalRegistrarFactory([new NoneFiscalRegistrarBuilder()]);

        var registrar = factory.GetFor(ConfigWith(null));

        Assert.Equal(NoneFiscalRegistrar.TypeName, registrar.RegistrarType);
    }

    [Fact]
    public void Resolves_registered_registrar_by_type_case_insensitively()
    {
        var factory = new FiscalRegistrarFactory([new NoneFiscalRegistrarBuilder(), new FakeVeriFactuBuilder()]);

        var registrar = factory.GetFor(ConfigWith("verifactu"));

        Assert.IsType<FakeVeriFactuRegistrar>(registrar);
    }

    [Fact]
    public void Builds_registrar_bound_to_the_requested_source_config()
    {
        var builder = new FakeVeriFactuBuilder();
        var factory = new FiscalRegistrarFactory([new NoneFiscalRegistrarBuilder(), builder]);
        var config = ConfigWith("VeriFactu");

        factory.GetFor(config);

        Assert.Same(config, builder.LastConfig);
    }

    [Fact]
    public void Throws_for_unknown_non_none_type()
    {
        var factory = new FiscalRegistrarFactory([new NoneFiscalRegistrarBuilder()]);

        Assert.Throws<InvalidOperationException>(() => factory.GetFor(ConfigWith("VeriFactu")));
    }

    [Fact]
    public async Task None_registrar_produces_no_stamp_and_accepts()
    {
        var none = new NoneFiscalRegistrar();
        var invoice = InvoiceBuilder.BuildIssued();

        var stamp = await none.StampAsync(invoice);
        var outcome = await none.SubmitAsync(invoice);

        Assert.False(stamp.HasStamp);
        Assert.Equal(FiscalSubmissionState.Accepted, outcome.State);
    }
}
