using CentralBillingService.Application.Exceptions;
using Microsoft.Data.SqlClient;

namespace CentralBillingService.Persistence.SqlServer.Contetxs;

/// <summary>
/// SQL Server implementation of IInvoiceWriteContext.
///
/// Concurrency strategy for ReserveNextNumberAsync:
/// The InvoiceSequences table uses a rowversion column (RowVersion).
/// EF's optimistic concurrency throws DbUpdateConcurrencyException if
/// two requests try to update the same row simultaneously.
/// We retry up to 3 times with a short delay — in practice collisions
/// are extremely rare since invoices are low-volume operations.
///
/// Invoices and rectificative invoices share the unified Invoices table.
/// InvoiceType = "F" for standard invoices, "R" for rectificative invoices.
/// </summary>
internal sealed class SqlInvoiceWriteContext(IOptions<DatabaseOptions> dbOptions) : DbContext, IInvoiceWriteContext
{
    private const int MaxRetries = 3;

    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();
    public DbSet<InvoiceSequenceEntity> InvoiceSequences => Set<InvoiceSequenceEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(dbOptions.Value.CbsDb,
           sqlOptions => sqlOptions.EnableRetryOnFailure(
               maxRetryCount: 3,
               maxRetryDelay: TimeSpan.FromSeconds(10),
               errorNumbersToAdd: null));
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ContextConfigurations.ConfigureInvoice(modelBuilder);
        ContextConfigurations.ConfigureInvoiceLine(modelBuilder);
        ContextConfigurations.ConfigureInvoiceSequence(modelBuilder);
    }

    public async Task<int> ReserveNextNumberAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var sequence = await InvoiceSequences
                    .FirstOrDefaultAsync(
                        x => x.BillingSource == billingSource &&
                             x.Serie == serie &&
                             x.Year == year,
                        cancellationToken);

                if (sequence is null)
                {
                    // First invoice for this BillingSource+Serie+Year
                    sequence = new InvoiceSequenceEntity
                    {
                        Id = Guid.NewGuid(),
                        BillingSource = billingSource,
                        Serie = serie,
                        Year = year,
                        LastNumber = 1,
                        LastHash = null,
                    };
                    InvoiceSequences.Add(sequence);
                    await SaveChangesAsync(cancellationToken);
                    return 1;
                }

                sequence.LastNumber++;
                await SaveChangesAsync(cancellationToken);
                return sequence.LastNumber;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                // Another request updated the row — refresh and retry
                ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Could not reserve invoice number for {billingSource}/{serie}/{year} " +
            $"after {MaxRetries} attempts due to concurrent requests.");
    }

    public async Task<Invoice> CreateAtomicAsync(
        string billingSource,
        string serie,
        int year,
        Func<int, string?, CancellationToken, Task<Invoice>> buildInvoice,
        CancellationToken cancellationToken = default)
    {
        // EnableRetryOnFailure is configured, so a user-initiated transaction must run inside
        // the execution strategy. The whole unit (lock → build → save → commit) is retried as
        // one on a transient failure; buildInvoice may therefore run more than once.
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Each attempt starts from a clean tracker: a prior rolled-back attempt may have left
            // the sequence row and a half-built invoice tracked, which we must not re-apply.
            ChangeTracker.Clear();

            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

            // Pessimistic, per-row lock on the sequence for this BillingSource+Serie+Year.
            // UPDLOCK + HOLDLOCK serializes concurrent writers for the SAME key (and range-locks
            // the key when the row does not exist yet, preventing two "first invoice" inserts),
            // while leaving other billing sources / series / years free to run in parallel.
            var sequence = await InvoiceSequences
                .FromSqlInterpolated(
                    $@"SELECT * FROM [InvoiceSequences] WITH (UPDLOCK, HOLDLOCK)
                       WHERE [BillingSource] = {billingSource} AND [Serie] = {serie} AND [Year] = {year}")
                .FirstOrDefaultAsync(cancellationToken);

            int reservedNumber;
            string? previousHash;
            if (sequence is null)
            {
                reservedNumber = 1;
                previousHash = null;
                sequence = new InvoiceSequenceEntity
                {
                    Id = Guid.NewGuid(),
                    BillingSource = billingSource,
                    Serie = serie,
                    Year = year,
                    LastNumber = 1,
                    LastHash = null,
                };
                InvoiceSequences.Add(sequence);
            }
            else
            {
                reservedNumber = sequence.LastNumber + 1;
                previousHash = sequence.LastHash;
                sequence.LastNumber = reservedNumber;
            }

            // Build the fully-hashed invoice from the reserved number and previous hash.
            var invoice = await buildInvoice(reservedNumber, previousHash, cancellationToken);

            // Advance the chain hash and insert the invoice — committed together with the
            // sequence increment in a single SaveChanges within this transaction.
            sequence.LastHash = invoice.Hash;
            Invoices.Add(InvoiceMapper.ToEntity(invoice));

            try
            {
                await SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsPaymentReferenceConflict(ex))
            {
                // A concurrent request persisted an invoice with the same payment reference first.
                // Surface a typed signal so the use case can return the existing one (idempotent).
                throw new DuplicatePaymentReferenceException(billingSource, invoice.PaymentReference, ex);
            }

            await transaction.CommitAsync(cancellationToken);
            return invoice;
        });
    }

    // SQL Server unique-index violation (2627 = constraint, 2601 = unique index) on the
    // filtered BillingSource + PaymentReference index.
    private static bool IsPaymentReferenceConflict(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && (sql.Number == 2601 || sql.Number == 2627)
        && sql.Message.Contains("PaymentReference", StringComparison.OrdinalIgnoreCase);

    public async Task SaveAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var entity = InvoiceMapper.ToEntity(invoice);
        Invoices.Add(entity);

        // Update the sequence row with the hash of this invoice
        // so GetLastHashAsync always returns the latest without scanning Invoices
        await UpdateSequenceHashAsync(
            invoice.BillingSource,
            invoice.Number.Serie,
            invoice.Number.Year,
            invoice.Hash,
            cancellationToken);

        await SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRectificativeAsync(
        RectificativeInvoice rectificative,
        Invoice updatedOriginal,
        CancellationToken cancellationToken = default)
    {
        // Insert the rectificative into the unified Invoices table
        var rectEntity = InvoiceMapper.ToEntity(rectificative);
        Invoices.Add(rectEntity);

        // Update the original's status and RectifiedBy — only those two fields
        var originalEntity = await Invoices
            .FirstOrDefaultAsync(
                x => x.Id == updatedOriginal.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Original invoice '{updatedOriginal.Number}' not found for rectification.");

        originalEntity.Status = updatedOriginal.Status.ToString();
        originalEntity.RectifiedByNumber = updatedOriginal.RectifiedBy?.Value;

        // Update the sequence hash for the rectificative serie
        await UpdateSequenceHashAsync(
            rectificative.BillingSource,
            rectificative.Number.Serie,
            rectificative.Number.Year,
            rectificative.Hash,
            cancellationToken);

        // Both changes committed in a single transaction
        await SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRectificativeFromRectificativeAsync(
        RectificativeInvoice rectificative,
        RectificativeInvoice updatedOriginal,
        CancellationToken cancellationToken = default)
    {
        var rectEntity = InvoiceMapper.ToEntity(rectificative);
        Invoices.Add(rectEntity);

        // Original rectificative is also in the unified Invoices table (InvoiceType = "R")
        var originalEntity = await Invoices
            .FirstOrDefaultAsync(x => x.Id == updatedOriginal.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Original rectificative invoice '{updatedOriginal.Number}' not found.");

        originalEntity.Status = updatedOriginal.Status.ToString();
        originalEntity.RectifiedByNumber = updatedOriginal.RectifiedBy?.Value;

        await UpdateSequenceHashAsync(
            rectificative.BillingSource,
            rectificative.Number.Serie,
            rectificative.Number.Year,
            rectificative.Hash,
            cancellationToken);

        await SaveChangesAsync(cancellationToken);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private async Task UpdateSequenceHashAsync(
        string billingSource,
        string serie,
        int year,
        string newHash,
        CancellationToken cancellationToken)
    {
        var sequence = await InvoiceSequences
            .FirstOrDefaultAsync(
                x => x.BillingSource == billingSource &&
                     x.Serie == serie &&
                     x.Year == year,
                cancellationToken);

        // The sequence row must exist — it was created in ReserveNextNumberAsync
        if (sequence is not null)
            sequence.LastHash = newHash;
    }
}
