namespace CentralBillingService.Tests.Unit.Application.UseCases;

/// <summary>Rectificatives must be issued in their own serie, never in one used by ordinary invoices.</summary>
public class RectificativeSerieGuardTests
{
    private static IInvoiceRepository RepositoryWhereSerieHasOrdinaryInvoices(string serie)
    {
        IInvoiceRepository repository = Substitute.For<IInvoiceRepository>();
        repository.IsSerieUsedByOrdinaryInvoicesAsync("shoupalbums", serie, Arg.Any<CancellationToken>()).Returns(true);
        return repository;
    }

    [Fact]
    public async Task Serie_of_ordinary_invoices_is_rejected_whatever_its_casing()
    {
        IInvoiceRepository repository = RepositoryWhereSerieHasOrdinaryInvoices("SUA");

        await Assert.ThrowsAsync<DomainException>(() =>
            RectificativeSerieGuard.EnsureSerieIsNotUsedByOrdinaryInvoicesAsync(repository, "shoupalbums", " sua ", CancellationToken.None));
    }

    [Fact]
    public async Task Own_rectificative_serie_is_accepted()
    {
        IInvoiceRepository repository = RepositoryWhereSerieHasOrdinaryInvoices("SUA");

        Exception? error = await Record.ExceptionAsync(() =>
            RectificativeSerieGuard.EnsureSerieIsNotUsedByOrdinaryInvoicesAsync(repository, "shoupalbums", "RSUA", CancellationToken.None));

        Assert.Null(error);
    }
}
