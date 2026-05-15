namespace CentralBillingService.Tests.Unit.Application.UseCases;

public class GetInvoiceUseCaseTests
{
    private readonly IInvoiceRepository _repository;
    private readonly GetInvoiceUseCase _useCase;

    public GetInvoiceUseCaseTests()
    {
        _repository = Substitute.For<IInvoiceRepository>();
        var registry = InvoiceBuilder.DefaultRegistry("web-fotos", "secret123");
        _useCase = new GetInvoiceUseCase(_repository, registry, new FakeInvoiceHasher());
    }

    private static GetInvoiceQuery ByNumber(string number) => new()
    {
        BillingSource = "web-fotos",
        Secret = "secret123",
        InvoiceNumber = number
    };

    private static GetInvoiceQuery ById(Guid id) => new()
    {
        BillingSource = "web-fotos",
        Secret = "secret123",
        Id = id
    };

    [Fact]
    public async Task ExecuteAsync_by_number_returns_invoice_result()
    {
        var invoice = InvoiceBuilder.BuildIssued(serie: "FOTO", number: 3, billingSource: "web-fotos");
        _repository
            .FindByNumberAsync("web-fotos", "FOTO2026-0003", Arg.Any<CancellationToken>())
            .Returns(invoice);

        var result = await _useCase.ExecuteAsync(ByNumber("FOTO2026-0003"));

        Assert.NotNull(result);
        Assert.Equal("FOTO2026-0003", result.InvoiceNumber);
    }

    [Fact]
    public async Task ExecuteAsync_by_id_returns_invoice_result()
    {
        var invoice = InvoiceBuilder.BuildIssued(billingSource: "web-fotos");
        _repository
            .FindByIdAsync("web-fotos", invoice.Id, Arg.Any<CancellationToken>())
            .Returns(invoice);

        var result = await _useCase.ExecuteAsync(ById(invoice.Id));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_invoice_not_found_by_number_throws()
    {
        _repository
            .FindByNumberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        await Assert.ThrowsAsync<InvoiceNotFoundException>(() =>
            _useCase.ExecuteAsync(ByNumber("FOTO2026-9999")));
    }

    [Fact]
    public async Task ExecuteAsync_invoice_not_found_by_id_throws()
    {
        _repository
            .FindByIdAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        await Assert.ThrowsAsync<InvoiceNotFoundException>(() =>
            _useCase.ExecuteAsync(ById(Guid.NewGuid())));
    }

    [Fact]
    public async Task ExecuteAsync_empty_billing_source_throws()
    {
        var query = new GetInvoiceQuery
        {
            BillingSource = "",
            Secret = "secret123",
            InvoiceNumber = "FOTO2026-0001"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(query));
    }

    [Fact]
    public async Task ExecuteAsync_no_id_or_number_throws()
    {
        var query = new GetInvoiceQuery
        {
            BillingSource = "web-fotos",
            Secret = "secret123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(query));
    }

    [Fact]
    public async Task ExecuteAsync_wrong_secret_throws()
    {
        var query = new GetInvoiceQuery
        {
            BillingSource = "web-fotos",
            Secret = "wrong-secret",
            InvoiceNumber = "FOTO2026-0001"
        };

        await Assert.ThrowsAsync<DomainException>(() => _useCase.ExecuteAsync(query));
    }

    [Fact]
    public async Task ExecuteAsync_result_maps_totals_correctly()
    {
        var lines = new List<InvoiceLine>
        {
            InvoiceLine.CreateInEur(1, "Foto", 1, Money.Of(200m, Currency.EUR), TaxRate.General)
        };
        var invoice = InvoiceBuilder.BuildIssued(billingSource: "web-fotos", lines: lines);
        _repository
            .FindByNumberAsync("web-fotos", invoice.Number.Value, Arg.Any<CancellationToken>())
            .Returns(invoice);

        var result = await _useCase.ExecuteAsync(ByNumber(invoice.Number.Value));

        Assert.Equal(200m, result.TaxableBaseEur.Amount);
        Assert.Equal(42m, result.TotalTaxAmountEur.Amount);
        Assert.Equal(242m, result.TotalEur.Amount);
    }
}
