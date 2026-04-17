namespace LogSystem.Core.Services.Azure;

public class AzureService
{
    public async Task UploadFileAsync(string systemName, string fileName, TimeSpan fileDuration, string content)
    {
        // TODO: Implement upload file to Azure Blob Storage
        // The file must have TTL of "fileDuration"
        // The file must be placed on "/logs/v1/{systemName}/{fileName}"
        // Content must be compressed using gzip
    }

    public async Task<DownloadedFile> DownloadFileAsync(string systemName, string fileName)
    {
        // TODO: Implement download file to Azure Blob Storage
        // The file must be placed on "/logs/v1/{systemName}/{fileName}"
        // Content from stream must be decompressed using gzip
    }

    public class DownloadedFile
    {
        public bool Found;
        public string? Content;
    }
}
