using System.Text;
using System.Text.Json;
using Aboitiz.Power.MobileAp.Core.Data.Abstractions;
using Aboitiz.Power.MobileAp.Core.Data.Diagnostics;
using Gluonics.Core.IoC;

namespace Aboitiz.Power.MobileAp.Core.Services.Diagnostics;
#nullable enable
[AutoRegister<IRequestFlowStorage>]
internal sealed class FileRequestFlowStorage : IRequestFlowStorage
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private readonly ICloudFileManager _cloudFileManager;

    private const string ROOT_FOLDER = "request-flow-traces";

    public FileRequestFlowStorage(ICloudFileManager cloudFileManager)
    {
        _cloudFileManager = cloudFileManager;
    }

    public async Task AppendAsync(
        RequestFlowEntry entry,
        CancellationToken cancellationToken = default)
    {
        string folderPath = Path.Combine(ROOT_FOLDER, $"{DateTime.UtcNow:yyyyMMdd}");


        string filePath = $"{entry.OperationId}.ndjson";

        byte[] serializedEntry;

        try
        {
            serializedEntry = JsonSerializer.SerializeToUtf8Bytes(entry, JsonSerializerOptions);
        }
        catch
        {
            return;
        }

        await _fileLock.WaitAsync(cancellationToken);

        try
        {
            byte[] existingFileContent = await ReadExistingFileAsync(
                folderPath,
                filePath,
                cancellationToken);

            byte[] newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

            byte[] combinedContent = CombineArrays(
                existingFileContent,
                newLineBytes,
                serializedEntry);

            await _cloudFileManager.UploadFile(
                folderPath,
                filePath,
                combinedContent);
        }
        catch
        {
            // best effort logging -> never throw
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<byte[]> ReadExistingFileAsync(
        string folderPath,
        string filePath,
        CancellationToken cancellationToken)
    {
        byte[] existingFileContent = [];

        try
        {
            byte[]? existingFileBytes = await _cloudFileManager.DownloadAsByteArrayAsync(
                folderPath,
                filePath) ?? [];

            if (existingFileBytes.Length > 0)
            {
                Stream existingFileStream = new MemoryStream(existingFileBytes);

                if (existingFileStream is not null)
                {
                    using MemoryStream memoryStream = new();

                    await existingFileStream.CopyToAsync(memoryStream, cancellationToken);

                    existingFileContent = memoryStream.ToArray();
                }
            }
        }
        catch
        {
            // file probably does not exist yet
        }

        return existingFileContent;
    }

    private static byte[] CombineArrays(
        byte[] existingContent,
        byte[] newLineBytes,
        byte[] newEntry)
    {
        bool hasExistingContent = existingContent.Length > 0;

        int totalLength = existingContent.Length + newEntry.Length;

        if (hasExistingContent)
        {
            totalLength += newLineBytes.Length;
        }

        byte[] combinedContent = new byte[totalLength];

        int offset = 0;

        if (hasExistingContent)
        {
            Buffer.BlockCopy(
                existingContent,
                0,
                combinedContent,
                offset,
                existingContent.Length);

            offset += existingContent.Length;

            Buffer.BlockCopy(
                newLineBytes,
                0,
                combinedContent,
                offset,
                newLineBytes.Length);

            offset += newLineBytes.Length;
        }

        Buffer.BlockCopy(
            newEntry,
            0,
            combinedContent,
            offset,
            newEntry.Length);

        return combinedContent;
    }
}