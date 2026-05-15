using Aboitiz.Power.MobileAp.Core.Data.Abstractions;
using Aboitiz.Power.MobileAp.Core.Data.Extensions;
using Gluonics.Core.IoC;
using Gluonics.Core.Logging;
using Gluonics.Core.RemoteStorage.S3;

namespace Aboitiz.Power.MobileAp.Core.Services.Assets;

[AutoRegister<ICloudFileManager>]
internal class CloudFileManager : ICloudFileManager, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly S3Client _client;
    private readonly S3ClientConfiguration _clientConfigurations;

    public CloudFileManager(
        S3ClientConfiguration clientConfigurations,
        ILogger logger1)
    {
        _logger = logger1;
        _clientConfigurations = clientConfigurations;
        _client = new S3Client(_clientConfigurations, new S3ClientOptions()
        {
            Delimiter = S3ClientOptions.Default.Delimiter,
            MaxConcurrentConnections = S3ClientOptions.Default.MaxConcurrentConnections,
            ThrowOnErrors = false,
            TimeOutSeconds = 5,
            MaxRetries = 1,
            LogErrorsOnly = true
        }, _logger);
    }

    string GetPath(string rootFolder, string fileName) =>
        Path.Combine(_clientConfigurations.RootFolder, rootFolder, Path.GetFileName(fileName)).Replace("\\", "/");

    public async Task DeleteFileAsync(string rootFolder, string fileName)
    {
        try
        {
            if (_client is not null)
            {
                string path = GetPath(rootFolder, fileName);
                await _client.DeleteFile(path);
            }
        }
        catch (Exception ex)
        {
            //_logger.Error($"Error deleteting file from {fileName}: {ex.Message}");
        }
    }

    public async Task<byte[]> DownloadAsByteArrayAsync(string rootFolder, string fileName)
    {
        byte[] result = Array.Empty<byte>();
        try
        {
            if (_client is not null)
            {
                string path = GetPath(rootFolder, fileName);
                using BinaryReader reader = await _client.DownloadAsBinary(path);
                result = reader.ToByteArray();
            }
        }
        catch (Exception ex)
        {
            //_logger.Error($"Error downloading file from {fileName}. {ex.Message}");
        }
        return result;
    }

    public async Task<string> DownloadAsTextAsync(string rootFolder, string fileName)
    {
        string result = string.Empty;
        try
        {
            if (_client is not null)
            {
                string path = GetPath(rootFolder, fileName);
                using var reader = await _client.DownloadAsText(path);
                if (reader is not null && reader.BaseStream is not null && reader.BaseStream.Length > 0)
                    result = await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex)
        {
            //_logger.Error($"Error downloading file from {fileName}. {ex.Message}");
        }
        return result;
    }

    public async Task UploadFile(string rootFolder, string fileName, byte[] bytes)
    {
        if (_client is not null)
        {
            string path = GetPath(rootFolder, fileName);
            try
            {
                using var stream = new MemoryStream(bytes);
                await _client.Upload(stream, path);
                //_logger.Information($"{fileName} - Uploaded");
            }
            catch (Exception ex)
            {
                //_logger.Error($"{fileName} upload exception: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
