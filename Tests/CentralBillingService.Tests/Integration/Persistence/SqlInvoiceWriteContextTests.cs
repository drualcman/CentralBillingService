namespace CentralBillingService.Tests.Integration.Persistence;

[Collection("CbsIntegration")]
public sealed class SqlInvoiceWriteContextTests(CbsDatabaseFixture fixture)
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private SqlInvoiceWriteContext NewWriteCtx() => new(fixture.Options);
    private SqlInvoiceReadContext NewReadCtx() => new(fixture.Options);

    private static string UniqueSrc() => $"test-{Guid.NewGuid():N}"[..16];
    private static string UniqueSerie() => Guid.NewGuid().ToString("N")[..4].ToUpper();

    private static readonly Sha256InvoiceHasher RealHasher = new();

    // Builds an issued invoice using the real SHA-256 hasher.
    private static Invoice BuildIssued(
        string serie, int number, string billingSource,
        string? previousHash = null,
        List<InvoiceLine>? lines = null,
        string paymentReference = "PAY-001") =>
        InvoiceBuilder.BuildIssued(
            serie: serie, number: number, billingSource: billingSource,
            hasher: RealHasher, previousHash: previousHash,
            lines: lines, paymentReference: paymentReference);

    private static bool IsValidSha256(string? s) =>
        s is { Length: 64 } && s.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

    // ── ReserveNextNumberAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ReserveNextNumber_first_call_returns_1()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var ctx = NewWriteCtx();

        var number = await ctx.ReserveNextNumberAsync(src, serie, 2026);

        Assert.Equal(1, number);
    }

    [Fact]
    public async Task ReserveNextNumber_second_call_increments_to_2()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var ctx = NewWriteCtx();

        await ctx.ReserveNextNumberAsync(src, serie, 2026);
        var second = await ctx.ReserveNextNumberAsync(src, serie, 2026);

        Assert.Equal(2, second);
    }

    [Fact]
    public async Task ReserveNextNumber_different_series_are_independent()
    {
        var src = UniqueSrc();
        var serieA = UniqueSerie();
        var serieB = UniqueSerie();
        await using var ctx = NewWriteCtx();

        var aFirst = await ctx.ReserveNextNumberAsync(src, serieA, 2026);
        var bFirst = await ctx.ReserveNextNumberAsync(src, serieB, 2026);
        var aSecond = await ctx.ReserveNextNumberAsync(src, serieA, 2026);

        Assert.Equal(1, aFirst);
        Assert.Equal(1, bFirst);
        Assert.Equal(2, aSecond);
    }

    [Fact]
    public async Task ReserveNextNumber_different_years_are_independent()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var ctx = NewWriteCtx();

        var y2026 = await ctx.ReserveNextNumberAsync(src, serie, 2026);
        var y2027 = await ctx.ReserveNextNumberAsync(src, serie, 2027);

        Assert.Equal(1, y2026);
        Assert.Equal(1, y2027);
    }

    [Fact]
    public async Task ReserveNextNumber_creates_sequence_row_in_db()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var ctx = NewWriteCtx();

        await ctx.ReserveNextNumberAsync(src, serie, 2026);

        var row = await ctx.InvoiceSequences
            .FirstOrDefaultAsync(x => x.BillingSource == src && x.Serie == serie && x.Year == 2026);
        Assert.NotNull(row);
        Assert.Equal(1, row.LastNumber);
    }

    // ── SaveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_persists_invoice_to_db()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, number, src);

        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);
        Assert.NotNull(found);
    }

    [Fact]
    public async Task SaveAsync_persists_invoice_lines()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        var lines = new List<InvoiceLine>
        {
            InvoiceBuilder.DefaultLine(1, 100m),
            InvoiceBuilder.DefaultLine(2, 200m),
        };
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, number, src, lines: lines);

        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);
        Assert.NotNull(found);
        Assert.Equal(2, found.Lines.Count);
    }

    [Fact]
    public async Task SaveAsync_persists_payment_reference()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, number, src, paymentReference: "PAY-INTG-001");

        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);
        Assert.NotNull(found);
        Assert.Equal("PAY-INTG-001", found.PaymentReference);
    }

    [Fact]
    public async Task SaveAsync_updates_sequence_last_hash()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, number, src);

        await writeCtx.SaveAsync(invoice);

        var sequence = await writeCtx.InvoiceSequences
            .FirstOrDefaultAsync(x => x.BillingSource == src && x.Serie == serie && x.Year == 2026);
        Assert.NotNull(sequence);
        Assert.Equal(invoice.Hash, sequence.LastHash);
    }

    [Fact]
    public async Task SaveAsync_preserves_invoice_totals()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateInEur(1, "Servicio", 1, Money.Of(200m, Currency.EUR), TaxRate.General)
        };
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, number, src, lines: lines);

        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);
        Assert.NotNull(found);
        Assert.Equal(200m, found.TaxableBaseEur.Amount);
        Assert.Equal(42m, found.TotalTaxAmountEur.Amount);
        Assert.Equal(242m, found.TotalEur.Amount);
    }

    [Fact]
    public async Task SaveAsync_hash_of_first_invoice_stored_in_sequence()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var n1 = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var inv1 = BuildIssued(serie, n1, src);
        await writeCtx.SaveAsync(inv1);

        await using var readCtx = NewReadCtx();
        var lastHash = await readCtx.GetLastHashAsync(src, serie, 2026);
        Assert.Equal(inv1.Hash, lastHash);
    }

    // ── VeriFactu hash chain ───────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_hash_is_valid_sha256_hex()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();
        var n = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, n, src);

        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);
        Assert.NotNull(found);
        Assert.True(IsValidSha256(found.Hash),
            $"Expected 64-char lowercase hex SHA-256, got: '{found.Hash}'");
    }

    [Fact]
    public async Task SaveAsync_first_invoice_has_null_previous_hash()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();
        var n = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = BuildIssued(serie, n, src, previousHash: null);

        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);
        Assert.NotNull(found);
        Assert.Null(found.PreviousHash);
    }

    [Fact]
    public async Task SaveAsync_second_invoice_previous_hash_equals_first_invoice_hash()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        // First invoice — no previous hash
        var n1 = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var inv1 = BuildIssued(serie, n1, src, previousHash: null);
        await writeCtx.SaveAsync(inv1);

        // Retrieve the hash that was stored (simulates what GetLastHashAsync returns in the use case)
        var lastHash = (await writeCtx.InvoiceSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BillingSource == src && x.Serie == serie && x.Year == 2026))!.LastHash;

        // Second invoice — previous hash = first invoice's hash
        var n2 = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var inv2 = BuildIssued(serie, n2, src, previousHash: lastHash);
        await writeCtx.SaveAsync(inv2);

        await using var readCtx = NewReadCtx();
        var found1 = await readCtx.FindByNumberAsync(src, inv1.Number.Value);
        var found2 = await readCtx.FindByNumberAsync(src, inv2.Number.Value);

        Assert.NotNull(found1);
        Assert.NotNull(found2);
        Assert.Null(found1.PreviousHash);
        Assert.Equal(found1.Hash, found2.PreviousHash);
    }

    [Fact]
    public async Task SaveAsync_hash_chain_three_invoices()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var ctx = NewWriteCtx();

        var n1 = await ctx.ReserveNextNumberAsync(src, serie, 2026);
        var inv1 = BuildIssued(serie, n1, src, previousHash: null);
        await ctx.SaveAsync(inv1);
        var hash1 = inv1.Hash;

        var n2 = await ctx.ReserveNextNumberAsync(src, serie, 2026);
        var inv2 = BuildIssued(serie, n2, src, previousHash: hash1);
        await ctx.SaveAsync(inv2);
        var hash2 = inv2.Hash;

        var n3 = await ctx.ReserveNextNumberAsync(src, serie, 2026);
        var inv3 = BuildIssued(serie, n3, src, previousHash: hash2);
        await ctx.SaveAsync(inv3);

        await using var readCtx = NewReadCtx();
        var r1 = await readCtx.FindByNumberAsync(src, inv1.Number.Value);
        var r2 = await readCtx.FindByNumberAsync(src, inv2.Number.Value);
        var r3 = await readCtx.FindByNumberAsync(src, inv3.Number.Value);

        Assert.Null(r1!.PreviousHash);
        Assert.Equal(r1.Hash, r2!.PreviousHash);
        Assert.Equal(r2.Hash, r3!.PreviousHash);
        Assert.True(IsValidSha256(r1.Hash));
        Assert.True(IsValidSha256(r2.Hash));
        Assert.True(IsValidSha256(r3.Hash));
        // Each invoice in the chain has a unique hash
        Assert.NotEqual(r1.Hash, r2.Hash);
        Assert.NotEqual(r2.Hash, r3.Hash);
    }

    [Fact]
    public async Task SaveAsync_hash_chain_is_independent_per_serie()
    {
        var src = UniqueSrc();
        var serieA = UniqueSerie();
        var serieB = UniqueSerie();
        await using var ctx = NewWriteCtx();

        var nA1 = await ctx.ReserveNextNumberAsync(src, serieA, 2026);
        var invA1 = BuildIssued(serieA, nA1, src, previousHash: null);
        await ctx.SaveAsync(invA1);

        var nA2 = await ctx.ReserveNextNumberAsync(src, serieA, 2026);
        var invA2 = BuildIssued(serieA, nA2, src, previousHash: invA1.Hash);
        await ctx.SaveAsync(invA2);

        var nB1 = await ctx.ReserveNextNumberAsync(src, serieB, 2026);
        var invB1 = BuildIssued(serieB, nB1, src, previousHash: null);
        await ctx.SaveAsync(invB1);

        await using var readCtx = NewReadCtx();
        var rA1 = await readCtx.FindByNumberAsync(src, invA1.Number.Value);
        var rA2 = await readCtx.FindByNumberAsync(src, invA2.Number.Value);
        var rB1 = await readCtx.FindByNumberAsync(src, invB1.Number.Value);

        Assert.Null(rA1!.PreviousHash);           // first in serie A
        Assert.Equal(rA1.Hash, rA2!.PreviousHash); // A chain
        Assert.Null(rB1!.PreviousHash);            // first in serie B — independent chain
    }

    // ── SaveRectificativeAsync ─────────────────────────────────────────────

    [Fact]
    public async Task SaveRectificativeAsync_persists_rectificative_invoice()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = BuildIssued(origSerie, origNumber, src);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);

        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindRectificativeByNumberAsync(src, rectificative.Number.Value);
        Assert.NotNull(found);
        Assert.Equal(rectificative.Number.Value, found.Number.Value);
    }

    [Fact]
    public async Task SaveRectificativeAsync_marks_original_as_rectified()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = BuildIssued(origSerie, origNumber, src);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);

        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var foundOriginal = await readCtx.FindByNumberAsync(src, original.Number.Value);
        Assert.NotNull(foundOriginal);
        Assert.Equal(InvoiceStatus.Rectified, foundOriginal.Status);
    }

    [Fact]
    public async Task SaveRectificativeAsync_stores_rectified_by_number_on_original()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = BuildIssued(origSerie, origNumber, src);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);

        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var foundOriginal = await readCtx.FindByNumberAsync(src, original.Number.Value);
        Assert.NotNull(foundOriginal);
        Assert.Equal(rectificative.Number, foundOriginal.RectifiedBy);
    }

    [Fact]
    public async Task SaveRectificativeAsync_persists_rectificative_lines()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = BuildIssued(origSerie, origNumber, src);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);

        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindRectificativeByNumberAsync(src, rectificative.Number.Value);
        Assert.NotNull(found);
        Assert.NotEmpty(found.Lines);
    }

    [Fact]
    public async Task SaveRectificativeAsync_hash_is_valid_sha256_hex()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = BuildIssued(origSerie, origNumber, src);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original,
            previousHash: original.Hash);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);
        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindRectificativeByNumberAsync(src, rectificative.Number.Value);
        Assert.NotNull(found);
        Assert.True(IsValidSha256(found.Hash),
            $"Expected 64-char lowercase hex SHA-256, got: '{found.Hash}'");
        Assert.Equal(original.Hash, found.PreviousHash);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static RectificativeInvoice BuildRectificative(
        string billingSource,
        string serie,
        int number,
        Invoice original,
        string? previousHash = null)
    {
        var rectNumber = InvoiceNumber.Create(serie, 2026, number);
        var lines = original.Lines.Select((l, i) =>
            InvoiceLine.CreateInEur(i + 1, l.Description, l.Quantity,
                Money.Of(-l.UnitPriceEur.Amount, Currency.EUR), l.TaxRate)).ToList();

        var rect = RectificativeInvoice.Create(
            rectNumber,
            billingSource,
            original,
            "Error en los datos del cliente - rectificación de integración",
            RectificationType.Substitution,
            lines,
            ExchangeRate.Identity(DateTimeOffset.UtcNow),
            RealHasher,
            paymentReference: "PAY-REC-001",
            previousHash: previousHash);
        rect.Issue();
        return rect;
    }

    private static Invoice CloneWithRectifiedStatus(Invoice original, InvoiceNumber rectNumber)
    {
        var clone = Invoice.Reconstitute(
            original.Id, original.Number, original.BillingSource,
            original.Issuer, original.Recipient,
            original.IssueDate, original.ValueDate, original.CreatedAt,
            original.Lines.ToList(), original.AppliedExchangeRate,
            original.Hash, original.PreviousHash,
            InvoiceStatus.Issued,
            original.PaymentReference,
            rectifiedBy: null, notes: original.Notes);
        clone.MarkAsRectifiedBy(rectNumber);
        return clone;
    }
}
