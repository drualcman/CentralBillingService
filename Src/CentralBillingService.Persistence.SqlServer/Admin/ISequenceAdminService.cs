namespace CentralBillingService.Persistence.SqlServer.Admin;

public interface ISequenceAdminService
{
    Task<List<SequenceInfo>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Crea o actualiza la secuencia (billingSource, serie, año) de modo que
    /// la próxima factura reciba el número <paramref name="startAt"/>.
    /// </summary>
    Task InitializeAsync(string billingSource, string serie, int year, int startAt, CancellationToken ct = default);

    Task DeleteAsync(string billingSource, string serie, int year, CancellationToken ct = default);
}

public record SequenceInfo(
    string BillingSource,
    string Serie,
    int Year,
    int LastNumber,
    bool HasInvoices);
