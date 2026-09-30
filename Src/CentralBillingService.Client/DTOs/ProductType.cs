namespace CentralBillingService.Client.DTOs;

/// <summary>
/// Economic nature of an invoice line: a service or a physical good. Mirrors the server enum
/// (same underlying values, so it round-trips over JSON). Drives fiscal classification of
/// foreign sales (e.g. VeriFactu not-subject services vs exempt exports).
/// </summary>
public enum ProductType
{
    Service = 0,
    Good = 1,
}
