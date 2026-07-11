namespace CentralBillingService.Persistence.SqlServer.Contetxs;

/// <summary>
/// SQL Server implementation of IInvoiceReadContext.
/// All queries are read-only — AsNoTracking throughout for performance.
/// Invoices and rectificative invoices are stored in the unified Invoices table
/// and distinguished by the InvoiceType column ("F" / "R").
/// </summary>
internal sealed class SqlInvoiceReadContext(IOptions<DatabaseOptions> dbOptions) : DbContext, IInvoiceReadContext
{
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

    public async Task<Invoice?> FindByIdAsync(string billingSource, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Invoices
            .AsNoTracking()
            .Where(s => s.BillingSource == billingSource && s.InvoiceType == "F")
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : InvoiceMapper.ToDomain(entity);
    }

    public async Task<Invoice?> FindByNumberAsync(
        string billingSource,
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        var entity = await Invoices
            .AsNoTracking()
            .Where(s => s.BillingSource == billingSource && s.InvoiceType == "F")
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.InvoiceNumber == invoiceNumber, cancellationToken);

        return entity is null ? null : InvoiceMapper.ToDomain(entity);
    }

    public async Task<Invoice?> FindByPaymentReferenceAsync(
        string billingSource,
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        var entity = await Invoices
            .AsNoTracking()
            .Where(s => s.BillingSource == billingSource && s.InvoiceType == "F")
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.PaymentReference == paymentReference, cancellationToken);

        return entity is null ? null : InvoiceMapper.ToDomain(entity);
    }

    public async Task<string?> GetLastHashAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default)
    {
        var sequence = await InvoiceSequences
            .AsNoTracking()
            .Where(s => s.BillingSource == billingSource)
            .FirstOrDefaultAsync(
                x => x.Serie == serie &&
                     x.Year == year,
                cancellationToken);

        return sequence?.LastHash;
    }

    public async Task<InvoicePagedResult> ListAsync(
        InvoiceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = Invoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.BillingSource))
            query = query.Where(x => x.BillingSource == filter.BillingSource);

        if (!string.IsNullOrWhiteSpace(filter.Serie))
            query = query.Where(x => x.Serie == filter.Serie);

        if (filter.Year.HasValue)
            query = query.Where(x => x.Year == filter.Year.Value);

        if (filter.IssuedFrom.HasValue)
            query = query.Where(x => x.IssueDate >= filter.IssuedFrom.Value);

        if (filter.IssuedTo.HasValue)
            query = query.Where(x => x.IssueDate <= filter.IssuedTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.RecipientTaxId))
            query = query.Where(x => x.RecipientTaxIdValue == filter.RecipientTaxId);

        if (!string.IsNullOrWhiteSpace(filter.RecipientExternalId))
            query = query.Where(x => x.RecipientExternalId == filter.RecipientExternalId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.Status == filter.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.SequenceNumber)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(x => x.Lines)
            .ToListAsync(cancellationToken);

        var invoices = new List<Invoice>();
        var rectificatives = new List<RectificativeInvoice>();

        foreach (var e in entities)
        {
            if (e.InvoiceType == "R")
                rectificatives.Add(InvoiceMapper.ToRectificativeDomain(e));
            else
                invoices.Add(InvoiceMapper.ToDomain(e));
        }

        return new InvoicePagedResult
        {
            Items = invoices,
            Rectificatives = rectificatives,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<IReadOnlyList<InvoiceSummaryDataPoint>> GetSummaryDataAsync(
        string? billingSource,
        CancellationToken cancellationToken = default)
    {
        return await Invoices.AsNoTracking()
            .Where(x => billingSource == null || x.BillingSource == billingSource)
            .Select(x => new InvoiceSummaryDataPoint(
                x.BillingSource,
                x.Year,
                x.IssueDate.Month,
                x.TotalEur,
                x.TaxableBaseEur,
                x.TotalTaxAmountEur))
            .ToListAsync(cancellationToken);
    }

    public async Task<RectificativeInvoice?> FindRectificativeByNumberAsync(
        string billingSource,
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        var entity = await Invoices
            .AsNoTracking()
            .Where(s => s.BillingSource == billingSource && s.InvoiceType == "R")
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.InvoiceNumber == invoiceNumber, cancellationToken);

        return entity is null ? null : InvoiceMapper.ToRectificativeDomain(entity);
    }
}
