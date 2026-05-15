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
    string GetBlobUrl(string blobName);

    /// <summary>
    /// Uploads <paramref name="content"/> under <paramref name="blobName"/> and
    /// returns the public URL where the blob can be accessed.
    /// </summary>
    Task<string> UploadAsync(
        string blobName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default);
}
