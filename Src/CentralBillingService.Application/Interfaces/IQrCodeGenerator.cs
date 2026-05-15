namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Generates a QR code image from a URL string.
///
/// The returned bytes are a PNG image ready to be stored or embedded in a PDF.
/// Implementations may use any QR library (QRCoder, ZXing, etc.).
/// The default implementation uses QRCoder with error-correction level Q.
/// </summary>
public interface IQrCodeGenerator
{
    Task<byte[]> GenerateAsync(string content, CancellationToken cancellationToken = default);
}
