using CentralBillingService.Application.DTOs;
using CentralBillingService.Application.UseCases;
using Microsoft.Extensions.Logging;

namespace CentralBillingService.Tests.Unit.Application.UseCases;

/// <summary>
/// The QR image must encode the fiscal authority's URL (AEAT ValidarQR) for fiscally-registered
/// invoices, and only fall back to the system verification URL when there is no fiscal registrar.
/// </summary>
public class GenerateInvoiceQrUseCaseTests
{
    private readonly IQrCodeGenerator _qr = Substitute.For<IQrCodeGenerator>();
    private readonly IBlobStorageService _blob = Substitute.For<IBlobStorageService>();
    private readonly IInvoiceVerificationUrlProvider _urlProvider = Substitute.For<IInvoiceVerificationUrlProvider>();
    private readonly IJobQueue _queue = Substitute.For<IJobQueue>();
    private readonly IIso9001 _iso = Substitute.For<IIso9001>();

    private GenerateInvoiceQrUseCase Build()
    {
        _qr.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([1, 2, 3]);
        _urlProvider.GetVerificationUrl(default!, default!, default!, default, default, default!)
            .ReturnsForAnyArgs("https://verify.cbs.com/v?x=1");
        return new GenerateInvoiceQrUseCase(
            _qr, _blob, _urlProvider, _queue,
            Substitute.For<ILogger<GenerateInvoiceQrUseCase>>(), _iso);
    }

    private static GenerateInvoiceQrCommand Command(string? fiscalQr) => new(
        "A/1", "src", "HASH", new DateOnly(2026, 7, 26), 100m, "B12345678", fiscalQr);

    [Fact]
    public async Task Encodes_the_AEAT_fiscal_url_when_present()
    {
        var useCase = Build();

        await useCase.ExecuteAsync(Command("https://aeat/ValidarQR?nif=X&numserie=A/1"));

        await _qr.Received(1).GenerateAsync("https://aeat/ValidarQR?nif=X&numserie=A/1", Arg.Any<CancellationToken>());
        _urlProvider.DidNotReceiveWithAnyArgs().GetVerificationUrl(default!, default!, default!, default, default, default!);
    }

    [Fact]
    public async Task Falls_back_to_system_url_when_no_fiscal_content()
    {
        var useCase = Build();

        await useCase.ExecuteAsync(Command(fiscalQr: null));

        await _qr.Received(1).GenerateAsync("https://verify.cbs.com/v?x=1", Arg.Any<CancellationToken>());
    }
}
