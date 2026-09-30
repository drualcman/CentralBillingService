using CentralBillingService.VeriFactu.Options;

namespace CentralBillingService.Tests.Unit.VeriFactu;

/// <summary>
/// VeriFactu config is per-billing-source: built from the source's Registrar.Settings bag.
/// The SIF producer identity defaults to the issuer (own-use software) unless overridden.
/// </summary>
public class VeriFactuOptionsTests
{
    [Fact]
    public void Empty_settings_use_cbs_defaults_and_issuer_as_producer()
    {
        var options = VeriFactuOptions.FromSettings(
            settings: null, issuerNif: "44714088H", issuerLegalName: "Mi Empresa SL");

        Assert.Equal("Pruebas", options.Environment);
        Assert.Equal("CentralBillingService", options.SistemaInformaticoNombre);
        Assert.Equal("01", options.SistemaInformaticoId);
        Assert.Equal("1.0", options.SistemaInformaticoVersion);
        Assert.Equal("1", options.SistemaInformaticoNumeroInstalacion);
        // Producer defaults to the issuer (in-house SIF).
        Assert.Equal("44714088H", options.SistemaInformaticoNif);
        Assert.Equal("Mi Empresa SL", options.SistemaInformaticoRazon);
    }

    [Fact]
    public void Settings_are_read_case_insensitively_and_override_defaults()
    {
        var settings = new Dictionary<string, string>
        {
            ["environment"] = "Produccion",
            ["CertificatePath"] = "C:\\SSL\\cert.pfx",
            ["SISTEMAINFORMATICOVERSION"] = "2.5",
            ["SistemaInformaticoNif"] = "B99999999", // third-party producer overrides the issuer
        };

        var options = VeriFactuOptions.FromSettings(settings, issuerNif: "44714088H", issuerLegalName: "Mi Empresa SL");

        Assert.True(options.IsProduction);
        Assert.Equal("C:\\SSL\\cert.pfx", options.CertificatePath);
        Assert.Equal("2.5", options.SistemaInformaticoVersion);
        Assert.Equal("B99999999", options.SistemaInformaticoNif);          // overridden
        Assert.Equal("Mi Empresa SL", options.SistemaInformaticoRazon);    // still defaulted
    }

    [Fact]
    public void Installation_number_defaults_to_billing_source_so_each_source_is_a_distinct_sif()
    {
        VeriFactuOptions shotUpOptions = VeriFactuOptions.FromSettings(
            settings: null, issuerNif: "44714088H", issuerLegalName: "Mi Empresa SL", billingSource: "shotupalbums");
        VeriFactuOptions surePeakOptions = VeriFactuOptions.FromSettings(
            settings: null, issuerNif: "44714088H", issuerLegalName: "Mi Empresa SL", billingSource: "surepeakinvest");

        Assert.Equal("shotupalbums", shotUpOptions.SistemaInformaticoNumeroInstalacion);
        Assert.Equal("surepeakinvest", surePeakOptions.SistemaInformaticoNumeroInstalacion);
    }

    [Fact]
    public void System_name_defaults_to_billing_source_so_each_source_reports_its_own_sif()
    {
        VeriFactuOptions options = VeriFactuOptions.FromSettings(settings: null, billingSource: "surepeakinvest");

        Assert.Equal("CBS_surepeakinvest", options.SistemaInformaticoNombre);
    }

    [Fact]
    public void Derived_system_name_is_truncated_to_the_aeat_maximum_of_30_characters()
    {
        string longBillingSource = new string('x', 45);

        VeriFactuOptions options = VeriFactuOptions.FromSettings(settings: null, billingSource: longBillingSource);

        Assert.Equal(30, options.SistemaInformaticoNombre.Length);
    }

    [Fact]
    public void System_id_longer_than_two_characters_is_rejected()
    {
        Dictionary<string, string> settings = new Dictionary<string, string>
        {
            ["SistemaInformaticoId"] = "SUA",
        };

        Assert.Throws<InvalidOperationException>(() =>
            VeriFactuOptions.FromSettings(settings, billingSource: "shotupalbums"));
    }

    [Fact]
    public void Explicit_installation_number_overrides_billing_source_default()
    {
        Dictionary<string, string> settings = new Dictionary<string, string>
        {
            ["SistemaInformaticoNumeroInstalacion"] = "SUA-01",
        };

        VeriFactuOptions options = VeriFactuOptions.FromSettings(settings, billingSource: "shotupalbums");

        Assert.Equal("SUA-01", options.SistemaInformaticoNumeroInstalacion);
    }
}
