namespace CentralBillingService.Tests.Integration.Persistence;

[Collection("CbsIntegration")]
public sealed class SqlInvoiceReadContextTests(CbsDatabaseFixture fixture)
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private SqlInvoiceWriteContext NewWriteCtx() => new(fixture.Options);
    private SqlInvoiceReadContext NewReadCtx() => new(fixture.Options);

    private static string UniqueSrc() => $"test-{Guid.NewGuid():N}"[..16];
    private static string UniqueSerie() => Guid.NewGuid().ToString("N")[..4].ToUpper();

    private static readonly Sha256InvoiceHasher RealHasher = new();

    private async Task<Invoice> SaveInvoiceAsync(
        string billingSource,
        string serie,
        List<InvoiceLine>? lines = null,
        DateOnly? issueDate = null,
        string? previousHash = null)
    {
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(billingSource, serie, 2026);
        var invoice = InvoiceBuilder.BuildIssued(
            serie: serie, number: number, billingSource: billingSource,
            lines: lines, hasher: RealHasher, previousHash: previousHash,
            issueDate: issueDate);
        await writeCtx.SaveAsync(invoice);
        return invoice;
    }

    private static bool IsValidSha256(string? s) =>
        s is { Length: 64 } && s.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

    // ── FindByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task FindByIdAsync_returns_invoice_for_matching_billing_source()
    {
        var src = UniqueSrc();
        var invoice = await SaveInvoiceAsync(src, UniqueSerie());

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByIdAsync(src, invoice.Id);

        Assert.NotNull(found);
        Assert.Equal(invoice.Id, found.Id);
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_for_wrong_billing_source()
    {
        var src = UniqueSrc();
        var invoice = await SaveInvoiceAsync(src, UniqueSerie());

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByIdAsync("other-source", invoice.Id);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_when_invoice_does_not_exist()
    {
        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByIdAsync("any-source", Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task FindByIdAsync_includes_lines()
    {
        var src = UniqueSrc();
        var lines = new List<InvoiceLine>
        {
            InvoiceBuilder.DefaultLine(1, 100m),
            InvoiceBuilder.DefaultLine(2, 200m),
        };
        var invoice = await SaveInvoiceAsync(src, UniqueSerie(), lines);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByIdAsync(src, invoice.Id);

        Assert.NotNull(found);
        Assert.Equal(2, found.Lines.Count);
    }

    [Fact]
    public async Task FindByIdAsync_restores_invoice_number()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        var invoice = await SaveInvoiceAsync(src, serie);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByIdAsync(src, invoice.Id);

        Assert.NotNull(found);
        Assert.Equal(invoice.Number.Value, found.Number.Value);
    }

    [Fact]
    public async Task FindByIdAsync_restores_payment_reference()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = InvoiceBuilder.BuildIssued(
            serie: serie, number: number, billingSource: src,
            hasher: RealHasher, paymentReference: "PAY-RESTORE-TEST");
        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByIdAsync(src, invoice.Id);

        Assert.NotNull(found);
        Assert.Equal("PAY-RESTORE-TEST", found.PaymentReference);
    }

    // ── FindByNumberAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FindByNumberAsync_returns_invoice_for_matching_number()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        var invoice = await SaveInvoiceAsync(src, serie);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);

        Assert.NotNull(found);
        Assert.Equal(invoice.Number.Value, found.Number.Value);
    }

    [Fact]
    public async Task FindByNumberAsync_returns_null_for_wrong_billing_source()
    {
        var src = UniqueSrc();
        var invoice = await SaveInvoiceAsync(src, UniqueSerie());

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync("wrong-source", invoice.Number.Value);

        Assert.Null(found);
    }

    [Fact]
    public async Task FindByNumberAsync_returns_null_when_number_does_not_exist()
    {
        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync("any-source", "NONEXISTENT2026-9999");

        Assert.Null(found);
    }

    [Fact]
    public async Task FindByNumberAsync_includes_lines()
    {
        var src = UniqueSrc();
        var lines = new List<InvoiceLine>
        {
            InvoiceBuilder.DefaultLine(1, 50m),
            InvoiceBuilder.DefaultLine(2, 75m),
            InvoiceBuilder.DefaultLine(3, 125m),
        };
        var invoice = await SaveInvoiceAsync(src, UniqueSerie(), lines);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);

        Assert.NotNull(found);
        Assert.Equal(3, found.Lines.Count);
    }

    [Fact]
    public async Task FindByNumberAsync_restores_issuer_and_recipient()
    {
        var src = UniqueSrc();
        var invoice = await SaveInvoiceAsync(src, UniqueSerie());

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);

        Assert.NotNull(found);
        Assert.Equal("Test Autónomo SA", found.Issuer.LegalName);
        Assert.Equal("ACME Corp SL", found.Recipient.LegalName);
    }

    [Fact]
    public async Task FindByNumberAsync_restores_exchange_rate()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateWithConversion(1, "Service", 1,
                Money.Of(100m, Currency.USD),
                Money.Of(92m, Currency.EUR),
                TaxRate.Zero)
        };
        await using var writeCtx = NewWriteCtx();
        var number = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
        var invoice = InvoiceBuilder.BuildIssued(
            serie: serie, number: number, billingSource: src,
            lines: lines, exchangeRate: rate, hasher: RealHasher);
        await writeCtx.SaveAsync(invoice);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindByNumberAsync(src, invoice.Number.Value);

        Assert.NotNull(found);
        Assert.Equal("USD", found.AppliedExchangeRate.From.Code);
        Assert.Equal(0.92m, found.AppliedExchangeRate.Rate);
    }

    // ── GetLastHashAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetLastHashAsync_returns_null_when_no_invoices_exist()
    {
        var src = UniqueSrc();

        await using var readCtx = NewReadCtx();
        var hash = await readCtx.GetLastHashAsync(src, UniqueSerie(), 2026);

        Assert.Null(hash);
    }

    [Fact]
    public async Task GetLastHashAsync_returns_hash_of_last_saved_invoice()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        var invoice = await SaveInvoiceAsync(src, serie);

        await using var readCtx = NewReadCtx();
        var hash = await readCtx.GetLastHashAsync(src, serie, 2026);

        Assert.Equal(invoice.Hash, hash);
    }

    [Fact]
    public async Task GetLastHashAsync_hash_is_valid_sha256_hex()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await SaveInvoiceAsync(src, serie);

        await using var readCtx = NewReadCtx();
        var hash = await readCtx.GetLastHashAsync(src, serie, 2026);

        Assert.True(IsValidSha256(hash),
            $"Expected 64-char lowercase hex SHA-256, got: '{hash}'");
    }

    [Fact]
    public async Task GetLastHashAsync_returns_latest_hash_after_multiple_saves()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();

        var inv1 = await SaveInvoiceAsync(src, serie);
        var inv2 = await SaveInvoiceAsync(src, serie, previousHash: inv1.Hash);

        await using var readCtx = NewReadCtx();
        var hash = await readCtx.GetLastHashAsync(src, serie, 2026);

        Assert.Equal(inv2.Hash, hash);
    }

    [Fact]
    public async Task GetLastHashAsync_is_independent_per_serie()
    {
        var src = UniqueSrc();
        var serieA = UniqueSerie();
        var serieB = UniqueSerie();

        var invA = await SaveInvoiceAsync(src, serieA);
        var invB = await SaveInvoiceAsync(src, serieB);

        await using var readCtx = NewReadCtx();
        var hashA = await readCtx.GetLastHashAsync(src, serieA, 2026);
        var hashB = await readCtx.GetLastHashAsync(src, serieB, 2026);

        Assert.Equal(invA.Hash, hashA);
        Assert.Equal(invB.Hash, hashB);
        Assert.NotEqual(hashA, hashB);
    }

    // ── ListAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_returns_invoices_for_billing_source()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await SaveInvoiceAsync(src, serie);
        await SaveInvoiceAsync(src, serie);

        await using var readCtx = NewReadCtx();
        var result = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListAsync_does_not_return_other_billing_sources()
    {
        var src = UniqueSrc();
        var other = UniqueSrc();
        await SaveInvoiceAsync(src, UniqueSerie());
        await SaveInvoiceAsync(other, UniqueSerie());

        await using var readCtx = NewReadCtx();
        var result = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal(src, i.BillingSource));
    }

    [Fact]
    public async Task ListAsync_filters_by_serie()
    {
        var src = UniqueSrc();
        var serie1 = UniqueSerie();
        var serie2 = UniqueSerie();
        await SaveInvoiceAsync(src, serie1);
        await SaveInvoiceAsync(src, serie2);

        await using var readCtx = NewReadCtx();
        var result = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Serie = serie1,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        Assert.StartsWith(serie1, result.Items[0].Number.Value);
    }

    [Fact]
    public async Task ListAsync_filters_by_year()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await SaveInvoiceAsync(src, serie);

        await using var readCtx = NewReadCtx();
        var result2026 = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Year = 2026,
            Page = 1,
            PageSize = 10
        });
        var result2027 = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Year = 2027,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result2026.TotalCount);
        Assert.Equal(0, result2027.TotalCount);
    }

    [Fact]
    public async Task ListAsync_filters_by_status_issued()
    {
        var src = UniqueSrc();
        await SaveInvoiceAsync(src, UniqueSerie());

        await using var readCtx = NewReadCtx();
        var issued = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Status = "Issued",
            Page = 1,
            PageSize = 10
        });
        var draft = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Status = "Draft",
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, issued.TotalCount);
        Assert.Equal(0, draft.TotalCount);
    }

    [Fact]
    public async Task ListAsync_paginates_correctly()
    {
        var src = UniqueSrc();
        var serie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        for (int i = 1; i <= 5; i++)
        {
            var n = await writeCtx.ReserveNextNumberAsync(src, serie, 2026);
            var inv = InvoiceBuilder.BuildIssued(
                serie: serie, number: n, billingSource: src, hasher: RealHasher);
            await writeCtx.SaveAsync(inv);
        }

        await using var readCtx = NewReadCtx();
        var page1 = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Page = 1,
            PageSize = 3
        });
        var page2 = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Page = 2,
            PageSize = 3
        });

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
    }

    [Fact]
    public async Task ListAsync_orders_by_issue_date_descending()
    {
        var src = UniqueSrc();
        var serieEarly = UniqueSerie();
        var serieLate = UniqueSerie();

        var earlyDate = new DateOnly(2026, 1, 10);
        var lateDate = new DateOnly(2026, 6, 20);

        await SaveInvoiceAsync(src, serieEarly, issueDate: earlyDate);
        await SaveInvoiceAsync(src, serieLate, issueDate: lateDate);

        await using var readCtx = NewReadCtx();
        var result = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(lateDate, result.Items[0].IssueDate);
        Assert.Equal(earlyDate, result.Items[1].IssueDate);
    }

    [Fact]
    public async Task ListAsync_filters_by_issue_date_range()
    {
        var src = UniqueSrc();
        var serieJan = UniqueSerie();
        var serieJun = UniqueSerie();

        await SaveInvoiceAsync(src, serieJan, issueDate: new DateOnly(2026, 1, 15));
        await SaveInvoiceAsync(src, serieJun, issueDate: new DateOnly(2026, 6, 15));

        await using var readCtx = NewReadCtx();
        var result = await readCtx.ListAsync(new InvoiceFilter
        {
            BillingSource = src,
            IssuedFrom = new DateOnly(2026, 2, 1),
            IssuedTo = new DateOnly(2026, 12, 31),
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(new DateOnly(2026, 6, 15), result.Items[0].IssueDate);
    }

    // ── FindRectificativeByNumberAsync ─────────────────────────────────────

    [Fact]
    public async Task FindRectificativeByNumberAsync_returns_rectificative()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = InvoiceBuilder.BuildIssued(
            serie: origSerie, number: origNumber, billingSource: src, hasher: RealHasher);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original,
            previousHash: original.Hash);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);
        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindRectificativeByNumberAsync(src, rectificative.Number.Value);

        Assert.NotNull(found);
        Assert.Equal(original.Number, found.OriginalInvoiceNumber);
    }

    [Fact]
    public async Task FindRectificativeByNumberAsync_returns_null_for_wrong_billing_source()
    {
        var src = UniqueSrc();
        var origSerie = UniqueSerie();
        var rectSerie = UniqueSerie();
        await using var writeCtx = NewWriteCtx();

        var origNumber = await writeCtx.ReserveNextNumberAsync(src, origSerie, 2026);
        var original = InvoiceBuilder.BuildIssued(
            serie: origSerie, number: origNumber, billingSource: src, hasher: RealHasher);
        await writeCtx.SaveAsync(original);

        var rectNumber = await writeCtx.ReserveNextNumberAsync(src, rectSerie, 2026);
        var rectificative = BuildRectificative(src, rectSerie, rectNumber, original);
        var updatedOriginal = CloneWithRectifiedStatus(original, rectificative.Number);
        await writeCtx.SaveRectificativeAsync(rectificative, updatedOriginal);

        await using var readCtx = NewReadCtx();
        var found = await readCtx.FindRectificativeByNumberAsync("wrong-source", rectificative.Number.Value);

        Assert.Null(found);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static RectificativeInvoice BuildRectificative(
        string billingSource, string serie, int number, Invoice original,
        string? previousHash = null)
    {
        var rectNumber = InvoiceNumber.Create(serie, 2026, number);
        var lines = original.Lines.Select((l, i) =>
            InvoiceLine.CreateInEur(i + 1, l.Description, l.Quantity,
                Money.Of(-l.UnitPriceEur.Amount, Currency.EUR), l.TaxRate)).ToList();

        var rect = RectificativeInvoice.Create(
            rectNumber, billingSource, original,
            "Error en los datos del cliente - test integración",
            RectificationType.Substitution,
            lines, ExchangeRate.Identity(DateTimeOffset.UtcNow),
            RealHasher, issueDate: new DateOnly(2026, 1, 15),
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
