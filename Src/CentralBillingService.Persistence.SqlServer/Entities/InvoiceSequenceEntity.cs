namespace CentralBillingService.Persistence.SqlServer.Entities;

/// <summary>
/// Tracks the current sequence counter for each BillingSource+Serie+Year combination.
/// Used by ReserveNextNumberAsync to atomically assign invoice numbers.
///
/// The LastHash column is also stored here so GetLastHashAsync can be served
/// from this single row without scanning the Invoices table.
///
/// Row is created on first invoice for a given combination and incremented
/// atomically thereafter using optimistic concurrency (RowVersion).
/// </summary>
public sealed class InvoiceSequenceEntity
{
    public Guid Id { get; set; }

    /// <summary>Composite natural key: BillingSource + Serie + Year</summary>
    public string BillingSource { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public int Year { get; set; }

    /// <summary>The last reserved sequence number. Starts at 0, first invoice gets 1.</summary>
    public int LastNumber { get; set; }

    /// <summary>
    /// Hash of the last issued invoice in this chain.
    /// Updated atomically alongside LastNumber in SaveAsync.
    /// Null until the first invoice is issued.
    /// </summary>
    public string? LastHash { get; set; }

    /// <summary>
    /// EF Core concurrency token — prevents two concurrent requests
    /// from reading and incrementing the same counter simultaneously.
    /// SQL Server maps this to a rowversion / timestamp column.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
