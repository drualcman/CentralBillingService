namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Builds an <see cref="IFiscalRegistrar"/> bound to a specific billing source's configuration.
///
/// A single registrar type (e.g. "VeriFactu") can serve many billing sources, each with its own
/// certificate, environment and endpoint (different companies/NIFs, possibly in different
/// countries). Rather than a single process-wide configuration, the factory asks the matching
/// builder to produce a registrar wired to <em>that</em> source's <c>Registrar.Settings</c>.
///
/// Each concrete adapter project registers one builder for its <see cref="RegistrarType"/>.
/// </summary>
public interface IFiscalRegistrarBuilder
{
    /// <summary>Discriminator matching RegistrarConfig.Type (e.g. "None", "VeriFactu").</summary>
    string RegistrarType { get; }

    /// <summary>Creates a registrar bound to this billing source's settings.</summary>
    IFiscalRegistrar Build(BillingSourceConfig config);
}
