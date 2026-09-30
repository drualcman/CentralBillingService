namespace CentralBillingService.Tests.Unit.Application.UseCases;

/// <summary>
/// A serie belongs to one (billing source, NIF): another billing source of the same NIF may not reuse
/// it in the same year, because the tax authority identifies an invoice by NIF + number.
/// </summary>
public class FiscalSerieGuardTests
{
    private const string IssuerNif = "12345678A"; // InvoiceBuilder.DefaultIssuer()

    private static BillingSourceConfig SourceConfig(string registrarType) => new BillingSourceConfig
    {
        BillingSource = "surepeakinvest",
        Secret = "s",
        Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer()),
        Registrar = new RegistrarConfig { Type = registrarType },
    };

    private static IInvoiceRepository RepositoryWhereSerieIsShared(bool isShared)
    {
        IInvoiceRepository repository = Substitute.For<IInvoiceRepository>();
        repository.IsSerieUsedByAnotherBillingSourceAsync(IssuerNif, "R", 2026, "surepeakinvest", Arg.Any<CancellationToken>())
            .Returns(isShared);
        return repository;
    }

    [Fact]
    public async Task Serie_used_by_another_source_of_the_same_nif_is_rejected_for_a_fiscal_source()
    {
        IInvoiceRepository repository = RepositoryWhereSerieIsShared(true);

        await Assert.ThrowsAsync<DomainException>(() =>
            FiscalSerieGuard.EnsureSerieIsNotSharedAsync(repository, SourceConfig("VeriFactu"), "R", 2026, CancellationToken.None));
    }

    [Fact]
    public async Task Serie_not_shared_is_accepted()
    {
        IInvoiceRepository repository = RepositoryWhereSerieIsShared(false);

        Exception? error = await Record.ExceptionAsync(() =>
            FiscalSerieGuard.EnsureSerieIsNotSharedAsync(repository, SourceConfig("VeriFactu"), "R", 2026, CancellationToken.None));

        Assert.Null(error);
    }

    [Fact]
    public async Task Source_without_fiscal_registrar_is_not_checked()
    {
        IInvoiceRepository repository = RepositoryWhereSerieIsShared(true);

        Exception? error = await Record.ExceptionAsync(() =>
            FiscalSerieGuard.EnsureSerieIsNotSharedAsync(repository, SourceConfig("None"), "R", 2026, CancellationToken.None));

        Assert.Null(error);
        await repository.DidNotReceiveWithAnyArgs().IsSerieUsedByAnotherBillingSourceAsync(default!, default!, default, default!, default);
    }
}
