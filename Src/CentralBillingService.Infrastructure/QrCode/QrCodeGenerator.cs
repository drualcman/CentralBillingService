using QRCoder;

namespace CentralBillingService.Infrastructure.QrCode;

/// <summary>
/// Generates QR code PNG images using the QRCoder library.
/// Error-correction level Q (≈25 % damage tolerance) — enough for printed invoices.
/// </summary>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public Task<byte[]> GenerateAsync(string content, CancellationToken cancellationToken = default)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        var png = code.GetGraphic(pixelsPerModule: 20);
        return Task.FromResult(png);
    }
}
