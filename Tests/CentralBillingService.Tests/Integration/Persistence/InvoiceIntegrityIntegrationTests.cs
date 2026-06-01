namespace CentralBillingService.Tests.Integration.Persistence;

/// <summary>
/// Verifies the end-to-end VeriFactu hash integrity chain:
/// save an invoice with a real SHA-256 hash → read it back → recompute hash
/// from stored fields → no mismatch means integrity is intact.
/// </summary>
[Collection("CbsIntegration")]
public sealed class InvoiceIntegrityIntegrationTests(CbsDatabaseFixture fixture)
{
    private SqlInvoiceWriteContext NewWriteCtx() => new(fixture.Options);
    private SqlInvoiceReadContext NewReadCtx() => new(fixture.Options);
    private InvoiceRepository NewRepository() => new(NewReadCtx(), NewWriteCtx());

    private static string UniqueSrc() => $"test-{Guid.NewGuid():N}"[..16];
    private static string UniqueSerie() => Guid.NewGuid().ToString("N")[..4].ToUpper();

    private static readonly Sha256InvoiceHasher RealHasher = new();

    private async Task<Invoice> SaveInvoiceAsync(string billingSource, string serie)
    {
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(billingSource, serie, 2026);
        var invoice = InvoiceBuilder.BuildIssued(
            serie: serie, number: number, billingSource: billingSource, hasher: RealHasher);
        await writeCtx.SaveAsync(invoice);
        return invoice;
    }

    // ── Hash integrity via repository read ────────────────────────────────

    [Fact]
    public async Task FindByIdAsync_returned_invoice_passes_integrity_check()
    {
        var src = UniqueSrc();
        var saved = await SaveInvoiceAsync(src, UniqueSerie());

        var repo = NewRepository();
        var found = await repo.FindByIdAsync(src, saved.Id);

        Assert.NotNull(found);
        Assert.Equal(saved.Id, found.Id);
        Assert.True(found.VerifyIntegrity(RealHasher));
    }

    [Fact]
    public async Task FindByNumberAsync_returned_invoice_passes_integrity_check()
    {
        var src = UniqueSrc();
        var saved = await SaveInvoiceAsync(src, UniqueSerie());

        var repo = NewRepository();
        var found = await repo.FindByNumberAsync(src, saved.Number.Value);

        Assert.NotNull(found);
        Assert.True(found.VerifyIntegrity(RealHasher));
    }

    [Fact]
    public async Task ListAsync_all_items_pass_integrity_check()
    {
        var src = UniqueSrc();
        await SaveInvoiceAsync(src, UniqueSerie());
        await SaveInvoiceAsync(src, UniqueSerie());

        var repo = NewRepository();
        var result = await repo.ListAsync(new InvoiceFilter { BillingSource = src });

        Assert.NotEmpty(result.Items);
        foreach (var item in result.Items)
            Assert.True(item.VerifyIntegrity(RealHasher),
                $"Integrity check failed for {item.Number.Value}");
    }

    // ── VerifyInvoiceIntegrityUseCase end-to-end ───────────────────────────

    [Fact]
    public async Task VerifyInvoiceIntegrityUseCase_returns_valid_for_saved_invoice()
    {
        var billingSource = UniqueSrc();
        var saved = await SaveInvoiceAsync(billingSource, UniqueSerie());

        var registry = InvoiceBuilder.DefaultRegistry(billingSource, "secret123");
        var repo = NewRepository();
        var useCase = new VerifyInvoiceIntegrityUseCase(repo, registry, RealHasher, Substitute.For<IIso9001>());

        var query = new VerifyInvoiceQuery
        {
            BillingSource = billingSource,
            InvoiceNumber = saved.Number.Value,
            ProvidedHash = saved.Hash
        };

        var result = await useCase.ExecuteAsync(query);

        Assert.True(result.IsValid);
        Assert.Equal(saved.Number.Value, result.InvoiceNumber);
        Assert.Equal(saved.Hash, result.Hash);
    }
}
