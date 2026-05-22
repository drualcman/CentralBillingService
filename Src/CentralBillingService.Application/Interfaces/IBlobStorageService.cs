namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Stores arbitrary blobs and returns a publicly accessible URL.
///
/// The default implementation targets Azure Blob Storage.
/// Implementations must ensure the blob is publicly readable
/// (no SAS token required) so QR images can be embedded in PDFs.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Returns the public URL for <paramref name="blobName"/> without uploading anything.
    /// The URL is deterministic and can be computed before the blob exists.
    /// </summary>
    string GetInvoiceUrl(string blobName);

    /// <summary>
    /// Returns the public URL for <paramref name="blobName"/> without uploading anything.
    /// The URL is deterministic and can be computed before the blob exists.
    /// </summary>
    string GetQrUrl(string blobName);

    /// <summary>
    /// Uploads <paramref name="content"/> under <paramref name="blobName"/> and
    /// returns the public URL where the blob can be accessed.
    /// </summary>
    Task<string> UploadQrAsync(
        string blobName,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads <paramref name="content"/> under <paramref name="blobName"/> and
    /// returns the public URL where the blob can be accessed.
    /// </summary>
    Task UploadInvoiceAsync(
        string blobName,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the invoice blob identified by <paramref name="blobName"/> and
    /// returns its raw bytes, or <see langword="null"/> if the blob does not exist.
    /// </summary>
    Task<byte[]?> DownloadInvoiceAsync(
        string blobName,
        CancellationToken cancellationToken = default);
}
