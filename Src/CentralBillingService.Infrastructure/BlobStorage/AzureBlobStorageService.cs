using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CentralBillingService.Infrastructure.BlobStorage;

/// <summary>
/// Stores blobs in Azure Blob Storage and returns public URLs.
/// The container is created with public blob access on first use.
/// </summary>
public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public AzureBlobStorageService(IOptions<CbsOptions> options)
    {
        _connectionString = options.Value.QrBlobConnectionString;
        _containerName = options.Value.QrBlobContainerName;
    }

    public string GetBlobUrl(string blobName)
    {
        var container = new BlobContainerClient(_connectionString, _containerName);
        return container.GetBlobClient(blobName).Uri.ToString();
    }

    public async Task<string> UploadAsync(
        string blobName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = new BlobContainerClient(_connectionString, _containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        using var stream = new MemoryStream(content);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);

        return blob.Uri.ToString();
    }
}
