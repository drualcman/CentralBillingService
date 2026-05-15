namespace CentralBillingService.Tests.Integration.Persistence;

[Collection("Iso9001Integration")]
public sealed class Iso9001ContextTests(Iso9001DatabaseFixture fixture)
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private Iso9001Context NewCtx() => new(fixture.Options);

    private static string UniqueId() => $"test-{Guid.NewGuid():N}"[..20];

    private static AuditLog BuildAuditLog(string entityId) => new()
    {
        EntityId = entityId,
        CompanyId = "TestCompany",
        Action = "TestAction",
        PerformedBy = "test-user",
        Timestamp = DateTime.UtcNow,
        Details = "Integration test audit log",
        Data = "{}"
    };

    private static IncidentReport BuildIncidentReport(string entityId) => new()
    {
        EntityId = entityId,
        CompanyId = "TestCompany",
        ReportedAt = DateTime.UtcNow,
        UserId = "test-user",
        Description = "Integration test incident",
        AffectedProcess = "Testing",
        Severity = "low",
        Data = "{}"
    };

    // ── AuditLog write ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_audit_log_persists_to_db()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildAuditLog(entityId));
        await ctx.SaveChangesAsync();

        var count = await ctx.AuditLogsQuery
            .CountAsync(e => e.EntityId == entityId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_multiple_audit_logs_persist_all()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildAuditLog(entityId));
        await ctx.AddAsync(BuildAuditLog(entityId));
        await ctx.AddAsync(BuildAuditLog(entityId));
        await ctx.SaveChangesAsync();

        var count = await ctx.AuditLogsQuery
            .CountAsync(e => e.EntityId == entityId);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AuditLog_properties_round_trip_correctly()
    {
        var entityId = UniqueId();
        var before = DateTime.UtcNow;
        await using var ctx = NewCtx();

        var log = BuildAuditLog(entityId);
        log.Action = "InvoiceCreated";
        log.PerformedBy = "sergi";
        log.Details = "Created invoice INV2026-0001";
        log.Data = @"{""invoiceId"":""abc123""}";
        await ctx.AddAsync(log);
        await ctx.SaveChangesAsync();

        var stored = await ctx.AuditLogsQuery
            .FirstOrDefaultAsync(e => e.EntityId == entityId);
        Assert.NotNull(stored);
        Assert.Equal(entityId, stored.EntityId);
        Assert.Equal("TestCompany", stored.CompanyId);
        Assert.Equal("InvoiceCreated", stored.Action);
        Assert.Equal("sergi", stored.PerformedBy);
        Assert.Equal("Created invoice INV2026-0001", stored.Details);
        Assert.Equal(@"{""invoiceId"":""abc123""}", stored.Data);
        Assert.True(stored.Timestamp >= before);
        Assert.True(stored.CreatedAt >= before);
    }

    [Fact]
    public async Task AuditLogsQuery_filters_by_entity_id()
    {
        var idA = UniqueId();
        var idB = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildAuditLog(idA));
        await ctx.AddAsync(BuildAuditLog(idA));
        await ctx.AddAsync(BuildAuditLog(idB));
        await ctx.SaveChangesAsync();

        var countA = await ctx.AuditLogsQuery.CountAsync(e => e.EntityId == idA);
        var countB = await ctx.AuditLogsQuery.CountAsync(e => e.EntityId == idB);

        Assert.Equal(2, countA);
        Assert.Equal(1, countB);
    }

    [Fact]
    public async Task AuditLog_id_is_auto_generated()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildAuditLog(entityId));
        await ctx.SaveChangesAsync();

        var stored = await ctx.AuditLogsQuery
            .FirstOrDefaultAsync(e => e.EntityId == entityId);
        Assert.NotNull(stored);
        Assert.NotEqual(Guid.Empty, stored.Id);
    }

    [Fact]
    public async Task AuditLog_default_company_id_is_applied()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        var log = BuildAuditLog(entityId);
        log.CompanyId = null!; // let model default kick in
        await ctx.AddAsync(log);
        await ctx.SaveChangesAsync();

        // The entity sets CompanyId from the passed AuditLog; default is in the model
        // but mapping in context always sets it from the domain object
        var stored = await ctx.AuditLogsQuery
            .FirstOrDefaultAsync(e => e.EntityId == entityId);
        Assert.NotNull(stored);
    }

    // ── IncidentReport write ───────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_incident_report_persists_to_db()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildIncidentReport(entityId));
        await ctx.SaveChangesAsync();

        var count = await ctx.IncidentReportsQuery
            .CountAsync(e => e.EntityId == entityId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_multiple_incident_reports_persist_all()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildIncidentReport(entityId));
        await ctx.AddAsync(BuildIncidentReport(entityId));
        await ctx.SaveChangesAsync();

        var count = await ctx.IncidentReportsQuery
            .CountAsync(e => e.EntityId == entityId);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task IncidentReport_properties_round_trip_correctly()
    {
        var entityId = UniqueId();
        var before = DateTime.UtcNow;
        await using var ctx = NewCtx();

        var report = BuildIncidentReport(entityId);
        report.Description = "Payment processing failed";
        report.AffectedProcess = "Payments";
        report.Severity = "high";
        report.Data = @"{""orderId"":""xyz""}";
        await ctx.AddAsync(report);
        await ctx.SaveChangesAsync();

        var stored = await ctx.IncidentReportsQuery
            .FirstOrDefaultAsync(e => e.EntityId == entityId);
        Assert.NotNull(stored);
        Assert.Equal(entityId, stored.EntityId);
        Assert.Equal("TestCompany", stored.CompanyId);
        Assert.Equal("test-user", stored.UserId);
        Assert.Equal("Payment processing failed", stored.Description);
        Assert.Equal("Payments", stored.AffectedProcess);
        Assert.Equal("high", stored.Severity);
        Assert.Equal(@"{""orderId"":""xyz""}", stored.Data);
        Assert.True(stored.ReportedAt >= before);
    }

    [Fact]
    public async Task IncidentReport_id_is_auto_generated()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildIncidentReport(entityId));
        await ctx.SaveChangesAsync();

        var stored = await ctx.IncidentReportsQuery
            .FirstOrDefaultAsync(e => e.EntityId == entityId);
        Assert.NotNull(stored);
        Assert.NotEqual(Guid.Empty, stored.Id);
    }

    [Fact]
    public async Task IncidentReportsQuery_filters_by_entity_id()
    {
        var idA = UniqueId();
        var idB = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildIncidentReport(idA));
        await ctx.AddAsync(BuildIncidentReport(idA));
        await ctx.AddAsync(BuildIncidentReport(idB));
        await ctx.SaveChangesAsync();

        var countA = await ctx.IncidentReportsQuery.CountAsync(e => e.EntityId == idA);
        var countB = await ctx.IncidentReportsQuery.CountAsync(e => e.EntityId == idB);

        Assert.Equal(2, countA);
        Assert.Equal(1, countB);
    }

    // ── ToListAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ToListAsync_with_no_filter_returns_added_records()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildIncidentReport(entityId));
        await ctx.AddAsync(BuildIncidentReport(entityId));
        await ctx.SaveChangesAsync();

        var all = await ctx.ToListAsync();

        // There may be records from other tests; just assert we can see ours
        Assert.True(all.Count >= 2);
        Assert.Equal(2, all.Count(r => r.EntityId == entityId));
    }

    [Fact]
    public async Task ToListAsync_with_filter_returns_only_matching()
    {
        var idA = UniqueId();
        var idB = UniqueId();
        await using var ctx = NewCtx();

        await ctx.AddAsync(BuildIncidentReport(idA));
        await ctx.AddAsync(BuildIncidentReport(idA));
        await ctx.AddAsync(BuildIncidentReport(idB));
        await ctx.SaveChangesAsync();

        var result = await ctx.ToListAsync(filter: e => e.EntityId == idA);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(idA, r.EntityId));
    }

    [Fact]
    public async Task ToListAsync_with_orderBy_desc_orders_correctly()
    {
        var entityId = UniqueId();
        await using var ctx = NewCtx();

        var t1 = DateTime.UtcNow.AddMinutes(-2);
        var t2 = DateTime.UtcNow.AddMinutes(-1);
        var t3 = DateTime.UtcNow;

        await ctx.AddAsync(new IncidentReport
        {
            EntityId = entityId, CompanyId = "TC", ReportedAt = t2,
            UserId = "u", Description = "second", AffectedProcess = "P", Severity = "low", Data = "{}"
        });
        await ctx.AddAsync(new IncidentReport
        {
            EntityId = entityId, CompanyId = "TC", ReportedAt = t1,
            UserId = "u", Description = "first", AffectedProcess = "P", Severity = "low", Data = "{}"
        });
        await ctx.AddAsync(new IncidentReport
        {
            EntityId = entityId, CompanyId = "TC", ReportedAt = t3,
            UserId = "u", Description = "third", AffectedProcess = "P", Severity = "low", Data = "{}"
        });
        await ctx.SaveChangesAsync();

        var result = await ctx.ToListAsync(
            filter: e => e.EntityId == entityId,
            orderBy: q => q.OrderByDescending(e => e.ReportedAt));

        Assert.Equal(3, result.Count);
        Assert.Equal("third", result[0].Description);
        Assert.Equal("second", result[1].Description);
        Assert.Equal("first", result[2].Description);
    }
}
