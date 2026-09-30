namespace CentralBillingService.VeriFactu.Options;

/// <summary>
/// Per-billing-source VeriFactu settings, built from that source's <c>Registrar.Settings</c>
/// bag (see <see cref="FromSettings"/>). Kept entirely inside this project so the rest of the app
/// stays authority-agnostic.
///
/// There is NO global VeriFactu configuration: each billing source carries its own certificate
/// and endpoint, so several companies/NIFs (Spain, and non-obligated ones elsewhere) can be
/// managed from a single host. The issuer NIF is taken from the billing source's issuer (it must
/// match the certificate holder), so it is not duplicated here.
/// </summary>
public sealed class VeriFactuOptions
{
    /// <summary>"Pruebas" (AEAT test portal, no tax effect) or "Produccion".</summary>
    public string Environment { get; set; } = "Pruebas";

    /// <summary>
    /// Optional explicit SOAP endpoint override. When empty, the endpoint is derived from
    /// <see cref="Environment"/>: Pruebas → prewww1, Produccion → www1 (personal certificate).
    /// For a sello-de-entidad certificate use the prewww10/www10 hosts.
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    // ── Certificate (persona física today; sello de entidad later, no code change) ─────────
    /// <summary>Path to a .pfx/.p12 client certificate. Used when set.</summary>
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
    /// <summary>Alternatively, select a certificate from the machine store by thumbprint.</summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    // ── SistemaInformatico (SIF identity — REQUIRED and validated by AEAT) ─────────────────
    // Each billing source is reported as its OWN SIF (independent business, own chain), so the
    // name and installation default to the billing source. The producer identity (NombreRazon +
    // NIF) defaults to the billing source's issuer (in-house/own-use) but can be overridden per
    // source when a third party developed the SIF.
    /// <summary>AEAT NombreSistemaInformatico (max 30 chars). Defaults to "CBS_{BillingSource}": same software, one SIF per business.</summary>
    public string SistemaInformaticoNombre { get; set; } = "CentralBillingService";
    /// <summary>AEAT IdSistemaInformatico (max 2 chars). Give each billing source its own code.</summary>
    public string SistemaInformaticoId { get; set; } = "01";
    public string SistemaInformaticoVersion { get; set; } = "1.0";
    /// <summary>
    /// Identifies this SIF installation at the AEAT. Each billing source is an independent SIF with
    /// its own chain, so it defaults to the billing source name to keep installations distinct.
    /// </summary>
    public string SistemaInformaticoNumeroInstalacion { get; set; } = "1";
    /// <summary>Software producer's legal name. Defaults to the issuer's legal name.</summary>
    public string SistemaInformaticoRazon { get; set; } = string.Empty;
    /// <summary>Software producer's NIF. Defaults to the issuer NIF (must match, for own-use SIF).</summary>
    public string SistemaInformaticoNif { get; set; } = string.Empty;

    public bool IsProduction => string.Equals(Environment, "Produccion", StringComparison.OrdinalIgnoreCase);

    // Settings-bag keys (kept here so config and reader never drift).
    public const string KeyEnvironment = "Environment";
    public const string KeyEndpointUrl = "EndpointUrl";
    public const string KeyCertificatePath = "CertificatePath";
    public const string KeyCertificatePassword = "CertificatePassword";
    public const string KeyCertificateThumbprint = "CertificateThumbprint";
    public const string KeySistemaInformaticoNombre = "SistemaInformaticoNombre";
    public const string KeySistemaInformaticoId = "SistemaInformaticoId";
    public const string KeySistemaInformaticoVersion = "SistemaInformaticoVersion";
    public const string KeySistemaInformaticoNumeroInstalacion = "SistemaInformaticoNumeroInstalacion";
    public const string KeySistemaInformaticoRazon = "SistemaInformaticoRazon";
    public const string KeySistemaInformaticoNif = "SistemaInformaticoNif";

    /// <summary>
    /// Builds the options from a billing source's opaque <c>Registrar.Settings</c> bag.
    /// Keys are matched case-insensitively; missing keys fall back to defaults. The SIF producer
    /// (razón/NIF) defaults to the issuer passed in, unless the bag overrides them.
    /// </summary>
    public static VeriFactuOptions FromSettings(
        IReadOnlyDictionary<string, string>? settings,
        string issuerNif = "",
        string issuerLegalName = "",
        string billingSource = "")
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (settings is not null)
            foreach (var kv in settings)
                lookup[kv.Key] = kv.Value;

        string Get(string key, string fallback = "") =>
            lookup.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

        bool hasBillingSource = !string.IsNullOrWhiteSpace(billingSource);
        string billingSourceSystemName = $"{SystemNamePrefix}{billingSource}";
        string defaultSystemName = hasBillingSource
            ? billingSourceSystemName[..Math.Min(billingSourceSystemName.Length, MaxSistemaInformaticoNombreLength)]
            : "CentralBillingService";

        VeriFactuOptions options = new VeriFactuOptions
        {
            Environment = Get(KeyEnvironment, "Pruebas"),
            EndpointUrl = Get(KeyEndpointUrl),
            CertificatePath = Get(KeyCertificatePath),
            CertificatePassword = Get(KeyCertificatePassword),
            CertificateThumbprint = Get(KeyCertificateThumbprint),
            SistemaInformaticoNombre = Get(KeySistemaInformaticoNombre, defaultSystemName),
            SistemaInformaticoId = Get(KeySistemaInformaticoId, "01"),
            SistemaInformaticoVersion = Get(KeySistemaInformaticoVersion, "1.0"),
            SistemaInformaticoNumeroInstalacion = Get(
                KeySistemaInformaticoNumeroInstalacion,
                hasBillingSource ? billingSource : "1"),
            SistemaInformaticoRazon = Get(KeySistemaInformaticoRazon, issuerLegalName),
            SistemaInformaticoNif = Get(KeySistemaInformaticoNif, issuerNif),
        };

        EnsureAeatFieldLengths(options);

        return options;
    }

    private const string SystemNamePrefix = "CBS_";
    private const int MaxSistemaInformaticoNombreLength = 30;
    private const int MaxSistemaInformaticoIdLength = 2;

    private static void EnsureAeatFieldLengths(VeriFactuOptions options)
    {
        if (options.SistemaInformaticoNombre.Length > MaxSistemaInformaticoNombreLength)
            throw new InvalidOperationException(
                $"{KeySistemaInformaticoNombre} '{options.SistemaInformaticoNombre}' exceeds the AEAT maximum of {MaxSistemaInformaticoNombreLength} characters.");

        if (options.SistemaInformaticoId.Length > MaxSistemaInformaticoIdLength)
            throw new InvalidOperationException(
                $"{KeySistemaInformaticoId} '{options.SistemaInformaticoId}' exceeds the AEAT maximum of {MaxSistemaInformaticoIdLength} characters.");
    }
}
