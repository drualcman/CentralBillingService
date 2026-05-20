namespace CentralBillingService.Domain.Services;

/// <summary>
/// Registra qué emisor (BillingParty) corresponde a cada origen de facturación.
///
/// Cada una de tus webs o proyectos puede presentarse con
/// un nombre comercial distinto, pero el NIF del emisor siempre
/// es el tuyo como autónomo declarado en España.
///
/// Este registro vive en el dominio — su configuración concreta
/// (leer de appsettings, BD, etc.) se inyecta desde infraestructura.
/// </summary>
public sealed class BillingSourceRegistry
{
    private readonly Dictionary<string, BillingSourceConfig> _configs;

    public BillingSourceRegistry(IOptions<CbsOptions> configs)
    {
        _configs = configs.Value.BillingSources
            .ToDictionary(
                c => c.BillingSource.ToLowerInvariant(),
                c => c);
    }

    /// <summary>
    /// Obtiene la configuración de emisor y serie para un origen dado.
    /// </summary>
    public BillingSourceConfig GetConfig(string billingSource, string secret)
    {
        var key = billingSource?.ToLowerInvariant()
            ?? throw new ArgumentNullException(nameof(billingSource));

        if (_configs.TryGetValue(key, out var config))
        {
            if (config.Secret.Equals(secret, StringComparison.Ordinal))
                return config;
        }
        throw new DomainException(
            $"Unrecognized billing source: '{billingSource}'. ");
    }

    /// <summary>
    /// Obtiene la configuración de emisor y serie para un origen dado.
    /// </summary>
    public BillingSourceConfig GetConfig(string billingSource)
    {
        var key = billingSource?.ToLowerInvariant()
            ?? throw new ArgumentNullException(nameof(billingSource));

        if (_configs.TryGetValue(key, out var config))
        {
            return config;
        }
        throw new DomainException(
            $"Unrecognized billing source: '{billingSource}'. ");
    }

    public bool IsRegistered(string billingSource) =>
        !string.IsNullOrWhiteSpace(billingSource) &&
        _configs.ContainsKey(billingSource.ToLowerInvariant());

    public IReadOnlyCollection<BillingSourceConfig> GetAll() => _configs.Values;
}