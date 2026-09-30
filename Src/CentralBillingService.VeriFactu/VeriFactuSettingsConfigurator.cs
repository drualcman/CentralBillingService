using VfSettings = VeriFactu.Config.Settings;
using VfSistemaInformatico = VeriFactu.Xml.Factu.SistemaInformatico;
using VfEndpoints = VeriFactu.VeriFactuEndPointPrefixes;

namespace CentralBillingService.VeriFactu;

/// <summary>
/// Applies a billing source's <see cref="VeriFactuOptions"/> to the mdiago library's
/// process-wide static <c>Settings.Current</c>.
///
/// The library's certificate/endpoint live in global static state, but CBS may serve several
/// billing sources with DIFFERENT certificates. So we do NOT configure once at startup; instead
/// the registrar applies the right source's certificate immediately before each library call,
/// serialized through <see cref="SyncRoot"/> so two concurrent submissions cannot clobber each
/// other's certificate. Submissions are I/O-bound and low-volume, so this serialization is cheap.
/// </summary>
public static class VeriFactuSettingsConfigurator
{
    /// <summary>Guards the shared static Settings.Current across billing sources. Hold it around
    /// (apply certificate → call the library) so the applied certificate is the one actually used.</summary>
    public static readonly object SyncRoot = new();

    /// <summary>Applies this source's certificate, endpoint and SIF identity to the shared static
    /// settings. Call inside a <see cref="SyncRoot"/> lock, right before invoking the library.</summary>
    public static void Apply(VeriFactuOptions options)
    {
        var s = VfSettings.Current;

        // Endpoint — CRITICAL: decides AEAT test portal vs production. Derived from Environment
        // using the library's own prefixes, unless an explicit EndpointUrl override is given.
        s.VeriFactuEndPointPrefix = !string.IsNullOrWhiteSpace(options.EndpointUrl)
            ? options.EndpointUrl
            : options.IsProduction ? VfEndpoints.Prod : VfEndpoints.Test;
        s.VeriFactuEndPointValidatePrefix = options.IsProduction
            ? VfEndpoints.ProdValidate : VfEndpoints.TestValidate;

        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            s.CertificatePath = options.CertificatePath;
            s.CertificatePassword = options.CertificatePassword;
            s.CertificateThumbprint = string.Empty; // path wins; clear the other selector
        }
        else if (!string.IsNullOrWhiteSpace(options.CertificateThumbprint))
        {
            s.CertificateThumbprint = options.CertificateThumbprint;
            s.CertificatePath = string.Empty;
            s.CertificatePassword = string.Empty;
        }

        // SistemaInformatico (SIF) — required by AEAT. The library builds it from
        // Settings.Current.SistemaInformatico, so we set this source's identity here. We mutate the
        // existing object to preserve the library's TipoUso/MultiOT flag defaults.
        var sif = s.SistemaInformatico ??= new VfSistemaInformatico();
        sif.NombreRazon = options.SistemaInformaticoRazon;
        sif.NIF = options.SistemaInformaticoNif;
        sif.NombreSistemaInformatico = options.SistemaInformaticoNombre;
        sif.IdSistemaInformatico = options.SistemaInformaticoId;
        sif.Version = options.SistemaInformaticoVersion;
        sif.NumeroInstalacion = options.SistemaInformaticoNumeroInstalacion;

        VfSettings.Save();
    }
}
