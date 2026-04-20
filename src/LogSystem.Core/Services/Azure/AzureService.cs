using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO.Compression;

namespace LogSystem.Core.Services.Azure;

public class AzureService
{
    private readonly AzureConfig AzureConfig;

    public AzureService(AzureConfig azureConfig)
    {
        AzureConfig = azureConfig;
    }

    public async Task UploadFileAsync(long logCollectionId, string fileName, TimeSpan fileDuration, string content)
    {
        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        // Get reference to blob at path /logs/v1/{logCollectionId}/{fileName}
        var blobPath = $"logs/v1/{logCollectionId}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Compress content using GZipStream to byte array
        byte[] compressedContent;
        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
            using (var writer = new StreamWriter(gzipStream))
            {
                await writer.WriteAsync(content);
            }
            compressedContent = outputStream.ToArray();
        }

        // Set blob metadata with TTL expiration based on fileDuration parameter
        // var expirationUtc = DateTime.UtcNow.Add(fileDuration);
        // var metadata = new Dictionary<string, string>
        // {
        //     { "ExpirationUtc", expirationUtc.ToString("o") }, // ISO 8601 format
        //     { "TTLHours", fileDuration.TotalHours.ToString("F2") }
        // };

        // Upload compressed content to blob with metadata
        // BlobUploadOptions allows overwriting existing blobs
        var uploadOptions = new BlobUploadOptions
        {
            // Metadata = metadata,
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/gzip",
                ContentEncoding = "gzip"
            }
        };

        try
        {
            using (var contentStream = new MemoryStream(compressedContent))
            {
                await blobClient.UploadAsync(contentStream, uploadOptions);
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            // Container doesn't exist, create it and retry upload
            await containerClient.CreateIfNotExistsAsync();

            using (var contentStream = new MemoryStream(compressedContent))
            {
                await blobClient.UploadAsync(contentStream, uploadOptions);
            }
        }
    }

    // TODO: Implement methods that can create, edit and delete lifecycle policy based on a collectionid.
    // The lifecycle specifies that files inside the collection folder must be deleted after X amount of time after creation.

    public async Task<DownloadedFile> DownloadFileAsync(long logCollectionId, string fileName)
    {
        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        // Get reference to blob at path /logs/v1/{logCollectionId}/{fileName}
        var blobPath = $"logs/v1/{logCollectionId}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        try
        {
            // Download blob content stream
            var downloadResponse = await blobClient.DownloadStreamingAsync();

            // Decompress using GZipStream
            string decompressedContent;
            using (var downloadStream = downloadResponse.Value.Content)
            using (var gzipStream = new GZipStream(downloadStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                decompressedContent = await reader.ReadToEndAsync();
            }

            // Return DownloadedFile with Found=true and Content populated
            return new DownloadedFile
            {
                Found = true,
                Content = decompressedContent
            };
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            // File not found, return a "not found" result
            return new DownloadedFile
            {
                Found = false,
                Content = null
            };
        }
    }

    public class DownloadedFile
    {
        public bool Found;
        public string? Content;
    }
}
