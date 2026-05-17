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
