namespace CentralBillingService.Persistence.SqlServer.Contetxs;

internal sealed class Iso9001Context(IOptions<DatabaseOptions> dbOptions) : DbContext, IIso9001Context
{
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<IncidentReportEntity> IncidentReports => Set<IncidentReportEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(dbOptions.Value.Iso9001Db,
           sqlOptions =>
           {
               sqlOptions.EnableRetryOnFailure(
                   maxRetryCount: 3,         // Número de intentos antes de fallar
                   maxRetryDelay: TimeSpan.FromSeconds(10),
                   errorNumbersToAdd: null
               );
           });
        base.OnConfiguring(optionsBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntity>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            builder.Property(x => x.EntityId).IsRequired();
            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.CompanyId).HasDefaultValue("ShotUpAlbums");
            builder.Property(x => x.Action).IsRequired();
            builder.Property(x => x.Timestamp).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<IncidentReportEntity>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.CompanyId).HasDefaultValue("ShotUpAlbums");
            builder.Property(x => x.EntityId).IsRequired();
            builder.Property(x => x.ReportedAt).IsRequired();
        });
    }

    public async Task<List<IncidentReportEntity>> ToListAsync(
        Expression<Func<IncidentReportEntity, bool>> filter = null,
        Func<IQueryable<IncidentReportEntity>, IOrderedQueryable<IncidentReportEntity>> orderBy = null)
    {
        IQueryable<IncidentReportEntity> query =
            IncidentReports
                   .AsNoTracking();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync();
    }


    public IQueryable<AuditLogEntity> AuditLogsQuery => AuditLogs.AsNoTracking();
    public IQueryable<IncidentReportEntity> IncidentReportsQuery => IncidentReports.AsNoTracking();

    public async Task AddAsync(AuditLog auditLog)
    {
        AuditLogEntity record = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EntityId = auditLog.EntityId,
            CompanyId = auditLog.CompanyId,
            Action = auditLog.Action,
            PerformedBy = auditLog.PerformedBy,
            Timestamp = auditLog.Timestamp,
            CreatedAt = DateTime.UtcNow,
            Details = auditLog.Details,
            Data = auditLog.Data
        };

        await AuditLogs.AddAsync(record);
    }

    public async Task AddAsync(IncidentReport incidentReport)
    {
        IncidentReportEntity entity = new IncidentReportEntity
        {
            Id = Guid.NewGuid(),
            EntityId = incidentReport.EntityId,
            CompanyId = incidentReport.CompanyId,
            ReportedAt = incidentReport.ReportedAt,
            UserId = incidentReport.UserId,
            Description = incidentReport.Description,
            AffectedProcess = incidentReport.AffectedProcess,
            Severity = incidentReport.Severity,
            Data = incidentReport.Data
        };

        await IncidentReports.AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await base.SaveChangesAsync();
    }
}

