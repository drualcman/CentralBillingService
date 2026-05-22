namespace CentralBillingService.Persistence.SqlServer.Admin;

public sealed class SequenceAdminService(IOptions<DatabaseOptions> dbOptions) : ISequenceAdminService
{
    public async Task<List<SequenceInfo>> GetAllAsync(CancellationToken ct = default)
    {
        await using var ctx = new SequenceAdminContext(dbOptions);
        return await ctx.Sequences
            .OrderBy(s => s.BillingSource)
            .ThenBy(s => s.Serie)
            .ThenBy(s => s.Year)
            .Select(s => new SequenceInfo(s.BillingSource, s.Serie, s.Year, s.LastNumber, s.LastHash != null))
            .ToListAsync(ct);
    }

    public async Task InitializeAsync(string billingSource, string serie, int year, int startAt, CancellationToken ct = default)
    {
        await using var ctx = new SequenceAdminContext(dbOptions);

        var existing = await ctx.Sequences.FirstOrDefaultAsync(
            s => s.BillingSource == billingSource && s.Serie == serie && s.Year == year, ct);

        if (existing is null)
        {
            ctx.Sequences.Add(new InvoiceSequenceEntity
            {
                Id = Guid.NewGuid(),
                BillingSource = billingSource,
                Serie = serie,
                Year = year,
                LastNumber = startAt - 1,
                LastHash = null,
            });
        }
        else
        {
            existing.LastNumber = startAt - 1;
        }

        await ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string billingSource, string serie, int year, CancellationToken ct = default)
    {
        await using var ctx = new SequenceAdminContext(dbOptions);

        var existing = await ctx.Sequences.FirstOrDefaultAsync(
            s => s.BillingSource == billingSource && s.Serie == serie && s.Year == year, ct);

        if (existing is not null)
        {
            ctx.Sequences.Remove(existing);
            await ctx.SaveChangesAsync(ct);
        }
    }
}

internal sealed class SequenceAdminContext(IOptions<DatabaseOptions> dbOptions) : DbContext
{
    public DbSet<InvoiceSequenceEntity> Sequences => Set<InvoiceSequenceEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder o) =>
        o.UseSqlServer(dbOptions.Value.CbsDb);

    protected override void OnModelCreating(ModelBuilder m) =>
        ContextConfigurations.ConfigureInvoiceSequence(m);
}
