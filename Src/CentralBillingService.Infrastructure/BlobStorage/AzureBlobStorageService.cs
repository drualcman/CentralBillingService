namespace CentralBillingService.Infrastructure.BlobStorage;

/// <summary>
/// Stores blobs in Azure Blob Storage and returns public URLs.
/// The container is created with public blob access on first use.
/// </summary>
public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerQrName;
    private readonly string _containerInvoiceName;

    public AzureBlobStorageService(IOptions<CbsOptions> options)
    {
        _connectionString = options.Value.QrBlobConnectionString;
        _containerQrName = options.Value.QrBlobContainerName;
        _containerInvoiceName = options.Value.Invoices;
    }

    public string GetInvoiceUrl(string blobName) =>
        GetBlobUrl(blobName, _containerInvoiceName);

    public string GetQrUrl(string blobName) =>
        GetBlobUrl(blobName, _containerQrName);

    public async Task<string> UploadQrAsync(
        string blobName,
        byte[] content,
        CancellationToken cancellationToken = default) =>
        await UploadAsync(blobName, _containerQrName, content, cancellationToken);

    public Task UploadInvoiceAsync(
        string blobName,
        byte[] content,
        CancellationToken cancellationToken = default) =>
        UploadAsync(blobName, _containerInvoiceName, content, cancellationToken);

    private string GetBlobUrl(string blobName, string containerName)
    {
        var container = new BlobContainerClient(_connectionString, containerName);
        return container.GetBlobClient(blobName).Uri.ToString();
    }

    private async Task<string> UploadAsync(
        string blobName,
        string containerName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var container = new BlobContainerClient(_connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        using var stream = new MemoryStream(content);
        await blob.UploadAsync(stream, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }
}
