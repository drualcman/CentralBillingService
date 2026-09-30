namespace CentralBillingService.Domain.Models;

/// <summary>
/// Selects which fiscal registrar (tax-authority reporting adapter) a billing source uses.
/// Mirrors <see cref="NumberProviderConfig"/>: the domain only knows the discriminator,
/// never the concrete registrar. The actual reporting logic (VeriFactu, CFDI, …) lives
/// entirely in its own adapter project, resolved by IFiscalRegistrarFactory.
/// </summary>
public sealed class RegistrarConfig
{
    /// <summary>The "no fiscal registration" discriminator (default).</summary>
    public const string None = "None";

    /// <summary>
    /// "None" (default — no fiscal registration) or a registered registrar type such as "VeriFactu".
    /// </summary>
    public string Type { get; set; } = None;

    /// <summary>
    /// Per-source registrar settings as an opaque key/value bag. The domain stays authority-agnostic:
    /// it never interprets these keys — the concrete adapter (VeriFactu, CFDI, …) reads the ones it
    /// needs (e.g. "CertificatePath", "Environment"). This is what lets several billing sources —
    /// possibly different companies/NIFs in different countries — each carry their own certificate and
    /// endpoint instead of a single process-wide configuration.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();

    /// <summary>True when this source performs no fiscal-authority reporting.</summary>
    public bool IsNone => string.IsNullOrWhiteSpace(Type) || string.Equals(Type, None, StringComparison.OrdinalIgnoreCase);
}
