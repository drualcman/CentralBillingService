namespace CentralBillingService.Domain.Models;

public record Attachment
{
    public string Name { get; set; }

    public byte[] Bytes { get; set; }

    public Attachment()
    {
        Name = string.Empty;
        Bytes = new byte[] { };
    }

    public Attachment(string fileName) : this() =>
        Name = fileName;
    public Attachment(string fileName, string folderContainer) : this(fileName)
    {
        string fullPath = Path.Combine(folderContainer, fileName);
        GetBytesFromFilePath(fullPath);
    }
    public Attachment(byte[] fileBytes) : this() =>
        Bytes = fileBytes;

    public Attachment(string fileName, byte[] fileBytes)
    {
        Name = fileName;
        Bytes = fileBytes;
    }

    public void GetBytesFromBase64(string base64) =>
        Bytes = Convert.FromBase64String(CleanBase64Format(base64));

    private string CleanBase64Format(string base64)
    {
        string[] getBase64 = base64.Split(',', StringSplitOptions.RemoveEmptyEntries);
        string toDecode;
        if (getBase64.Length > 1)
        {
            toDecode = getBase64[1];
        }
        else
            toDecode = base64;
        return toDecode;
    }

    public static byte[] StringToBytes(string content)
    {
        UTF8Encoding encoding = new UTF8Encoding();
        return encoding.GetBytes(content);
    }

    public void GetBytesFromFilePath(string fullFilePathAndName) =>
        Bytes = File.ReadAllBytes(fullFilePathAndName);

    public string GetFileName() =>
        Path.GetFileName(Name);
    public string GetFileNameWithoutExtension() =>
        Path.GetFileNameWithoutExtension(Name);
    public string GetExtension() =>
        Path.GetExtension(Name);
    public string GetFullPath() =>
        Path.GetFullPath(Name);

    public Stream ToStream() => new MemoryStream(Bytes);

    public string ToBase64()
    {
        string result = string.Empty;
        if (Bytes != null && Bytes.Length > 0)
            result = Convert.ToBase64String(Bytes);
        return result;
    }

    public override string ToString() =>
        ToBase64();
}
