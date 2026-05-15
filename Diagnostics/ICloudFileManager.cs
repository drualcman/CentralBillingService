namespace Aboitiz.Power.MobileAp.Core.Data.Abstractions;

public interface ICloudFileManager
{
    Task UploadFile(string rootFolder, string fileName, byte[] bytes);
    Task<string> DownloadAsTextAsync(string rootFolder, string fileName);
    Task<byte[]> DownloadAsByteArrayAsync(string rootFolder, string fileName);
    Task DeleteFileAsync(string rootFolder, string fileName);
}
