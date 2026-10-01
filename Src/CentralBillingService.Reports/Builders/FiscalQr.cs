namespace CentralBillingService.Reports.Builders;

/// <summary>
/// The invoice QR image. For a fiscally-registered invoice (VeriFactu) it encodes the AEAT ValidarQR URL
/// and must be printed at the top of the first page with the "VERI*FACTU" legend (Orden HAC/1177/2024).
/// </summary>
internal static class FiscalQr
{
    public const string LegendText = "VERI*FACTU";

    public static bool IsPrinted(Invoice invoice) =>
        !string.IsNullOrWhiteSpace(invoice.FiscalQrContent) && !string.IsNullOrWhiteSpace(invoice.QrCodeBlobUrl);

    public static async Task<byte[]> LoadImageAsync(Invoice invoice)
    {
        byte[] image = [];
        if (!string.IsNullOrWhiteSpace(invoice.QrCodeBlobUrl))
            image = await DownloadUrlHelper.GetBytes(invoice.QrCodeBlobUrl);
        return image;
    }
}
