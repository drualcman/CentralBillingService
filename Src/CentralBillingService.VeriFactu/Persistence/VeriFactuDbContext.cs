namespace CentralBillingService.VeriFactu.Persistence;

/// <summary>
/// EF Core context owned entirely by the VeriFactu project: the authoritative AEAT huella
/// chain (per NIF and billing source) and per-invoice submission state. Isolated from the main billing schema —
/// the rest of the app never sees these tables.
/// </summary>
public sealed class VeriFactuDbContext : DbContext
{
    public VeriFactuDbContext(DbContextOptions<VeriFactuDbContext> options) : base(options) { }

    public DbSet<VeriFactuChainEntity> Chains => Set<VeriFactuChainEntity>();
    public DbSet<VeriFactuSubmissionEntity> Submissions => Set<VeriFactuSubmissionEntity>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<VeriFactuChainEntity>(e =>
        {
            e.ToTable("VeriFactuChain");
            e.HasKey(x => new { x.Nif, x.BillingSource });
            e.Property(x => x.Nif).HasMaxLength(20);
            e.Property(x => x.BillingSource).HasMaxLength(50);
            e.Property(x => x.LastNumSerie).HasMaxLength(60);
            e.Property(x => x.LastFechaExpedicion).HasMaxLength(10);
            e.Property(x => x.LastHuella).HasMaxLength(64);
            e.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        mb.Entity<VeriFactuSubmissionEntity>(e =>
        {
            e.ToTable("VeriFactuSubmission");
            e.HasKey(x => x.Id);
            e.Property(x => x.BillingSource).HasMaxLength(50).IsRequired();
            e.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.IssuerNif).HasMaxLength(20).IsRequired();
            e.Property(x => x.Huella).HasMaxLength(64);
            e.Property(x => x.PreviousHuella).HasMaxLength(64);
            e.Property(x => x.PreviousNumSerie).HasMaxLength(60);
            e.Property(x => x.PreviousFechaExpedicion).HasMaxLength(10);
            e.Property(x => x.FechaHoraGenRegistro).HasMaxLength(30);
            e.Property(x => x.Csv).HasMaxLength(50);
            e.Property(x => x.ErrorCode).HasMaxLength(20);
            e.Property(x => x.ErrorDescription).HasMaxLength(500);
            e.HasIndex(x => new { x.BillingSource, x.InvoiceNumber }).IsUnique();
            e.HasIndex(x => x.State);
        });
    }
}
