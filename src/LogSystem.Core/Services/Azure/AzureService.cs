using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
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

    /// <summary>
    /// Creates a lifecycle management policy for a specific log collection.
    /// Files in the collection folder will be deleted after the specified duration.
    /// </summary>
    /// <param name="logCollectionId">The ID of the log collection</param>
    /// <param name="retentionHours">Number of hours to retain files before deletion</param>
    public async Task CreateLifecyclePolicyAsync(long logCollectionId, long retentionHours)
    {
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);

        var managementPolicy = await GetOrCreateManagementPolicyAsync(storageAccount);
        var policyData = managementPolicy.Data;

        // Create rule name: logcollection-{logCollectionId}-lifecycle
        var ruleName = $"logcollection-{logCollectionId}-lifecycle";

        // Check if rule already exists
        if (policyData.Rules.Any(r => r.Name == ruleName))
        {
            throw new InvalidOperationException($"Lifecycle policy for log collection {logCollectionId} already exists.");
        }

        // Create new rule for this collection
        var definition = new ManagementPolicyDefinition(new ManagementPolicyAction
        {
            BaseBlob = new ManagementPolicyBaseBlob
            {
                Delete = new DateAfterModification
                {
                    DaysAfterModificationGreaterThan = (int)Math.Ceiling(retentionHours / 24.0)
                }
            }
        })
        {
            Filters = new ManagementPolicyFilter(new[] { "blockBlob" })
            {
                PrefixMatch = { $"logs/v1/{logCollectionId}/" }
            }
        };

        var rule = new ManagementPolicyRule(ruleName, ManagementPolicyRuleType.Lifecycle, definition)
        {
            IsEnabled = true
        };

        policyData.Rules.Add(rule);

        // Update the management policy
        await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
    }

    /// <summary>
    /// Updates the lifecycle management policy for a specific log collection.
    /// </summary>
    /// <param name="logCollectionId">The ID of the log collection</param>
    /// <param name="retentionHours">New number of hours to retain files before deletion</param>
    public async Task UpdateLifecyclePolicyAsync(long logCollectionId, long retentionHours)
    {
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);

        var managementPolicy = await GetOrCreateManagementPolicyAsync(storageAccount);
        var policyData = managementPolicy.Data;

        var ruleName = $"logcollection-{logCollectionId}-lifecycle";

        // Find the existing rule
        var existingRule = policyData.Rules.FirstOrDefault(r => r.Name == ruleName);
        if (existingRule == null)
        {
            throw new InvalidOperationException($"Lifecycle policy for log collection {logCollectionId} does not exist.");
        }

        // Update the retention period
        if (existingRule.Definition.Actions.BaseBlob?.Delete != null)
        {
            existingRule.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan = (int)Math.Ceiling(retentionHours / 24.0);
        }

        // Update the management policy
        await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
    }

    /// <summary>
    /// Deletes the lifecycle management policy for a specific log collection.
    /// </summary>
    /// <param name="logCollectionId">The ID of the log collection</param>
    public async Task DeleteLifecyclePolicyAsync(long logCollectionId)
    {
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);

        var managementPolicy = await GetOrCreateManagementPolicyAsync(storageAccount);
        var policyData = managementPolicy.Data;

        var ruleName = $"logcollection-{logCollectionId}-lifecycle";

        // Find and remove the rule
        var existingRule = policyData.Rules.FirstOrDefault(r => r.Name == ruleName);
        if (existingRule == null)
        {
            throw new InvalidOperationException($"Lifecycle policy for log collection {logCollectionId} does not exist.");
        }

        policyData.Rules.Remove(existingRule);

        // Update the management policy (or delete if no rules remain)
        if (policyData.Rules.Count > 0)
        {
            await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
        }
        else
        {
            await managementPolicy.DeleteAsync(WaitUntil.Completed);
        }
    }

    private ArmClient GetArmClient()
    {
        var credential = new ClientSecretCredential(
            AzureConfig.TenantId,
            AzureConfig.ClientId,
            AzureConfig.ClientSecret
        );
        return new ArmClient(credential);
    }

    private async Task<StorageAccountResource> GetStorageAccountAsync(ArmClient armClient)
    {
        var subscriptionId = AzureConfig.SubscriptionId;
        var resourceGroupName = AzureConfig.ResourceGroupName;
        var storageAccountName = AzureConfig.StorageAccountName;

        var resourceId = StorageAccountResource.CreateResourceIdentifier(
            subscriptionId,
            resourceGroupName,
            storageAccountName
        );

        var storageAccount = armClient.GetStorageAccountResource(resourceId);

        // Verify the storage account exists
        await storageAccount.GetAsync();

        return storageAccount;
    }

    private async Task<StorageAccountManagementPolicyResource> GetOrCreateManagementPolicyAsync(StorageAccountResource storageAccount)
    {
        try
        {
            return await storageAccount.GetStorageAccountManagementPolicy().GetAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Management policy doesn't exist, create a new one
            var policyData = new StorageAccountManagementPolicyData();

            var result = await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
            return result.Value;
        }
    }

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
