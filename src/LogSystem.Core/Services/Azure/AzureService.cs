using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LogSystem.Core.Services.Database;
using LogSystem.Core.Metrics;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using Azure.Storage;
using System.Text;
using LogSystem.Core.Services.Common;
using LogSystem.Core.Services.Common.Compression;

namespace LogSystem.Core.Services.Azure;

public class AzureService
{
    // If any is found, create lazy load methods for them to ensure single instance and update consumer codes accordingly.
    private readonly AzureConfig AzureConfig;
    private readonly CompressionFactory _compressionFactory;

    public AzureService(AzureConfig azureConfig, CompressionFactory compressionFactory)
    {
        AzureConfig = azureConfig;
        _compressionFactory = compressionFactory;
    }

    public async Task UploadFileAsync(string collectionName, string fileName, string content, ICompressionStrategy compressionStrategy, AzureOperationReport azureReport)
    {
        var totalStopwatch = Stopwatch.StartNew();

        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        // Get reference to blob at path /logs/v1/{collectionName}/{fileName}
        var blobPath = GenerateBlobPath(collectionName, fileName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Compress content using the provided compression strategy
        var compressStopwatch = Stopwatch.StartNew();

        var originalBytes = Encoding.UTF8.GetBytes(content);
        var originalSizeBytes = originalBytes.Length;
        
        var compressedBytes = compressionStrategy.Compress(originalBytes);
        var compressedSizeBytes = compressedBytes.Length;

        // Calculate compression reduction ratio (e.g., 0.745 = 74.5% reduction)
        azureReport.CompressionReductionRatio = Math.Round(1.0 - ((double)compressedSizeBytes / originalSizeBytes), 3);
        azureReport.CompressData = compressStopwatch.StopAndReturnEllapsed();

        // Upload compressed content to blob with metadata
        // BlobUploadOptions allows overwriting existing blobs
        var uploadOptions = new BlobUploadOptions
        {
            // Metadata = metadata,
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = compressionStrategy.GetMimeContentType(),
                ContentEncoding = compressionStrategy.GetContentEncoding()
            },

            TransferOptions = new StorageTransferOptions
            {
                MaximumConcurrency = 50,
                MaximumTransferSize = null
            }
        };

        var uploadStopwatch = Stopwatch.StartNew();

        try
        {
            using var outputStream = new MemoryStream(compressedBytes);
            await blobClient.UploadAsync(outputStream, uploadOptions);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerNotFound")
        {
            // Container doesn't exist, create it and retry upload
            await containerClient.CreateIfNotExistsAsync();

            using var outputStream = new MemoryStream(compressedBytes);
            await blobClient.UploadAsync(outputStream, uploadOptions);
        }

        azureReport.UploadFile = uploadStopwatch.StopAndReturnEllapsed();
        azureReport.TotalExecutionTime = totalStopwatch.StopAndReturnEllapsed();
    }

    public async Task SaveLifecyclePolicyAsync(LogCollection logCollection)
    {
        var policyName = $"logcollection-{logCollection.TableName}-lifecycle";

        // Create or update Azure lifecycle policy
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);
        var managementPolicy = await GetManagementPolicyAsync(storageAccount);

        var policyData = managementPolicy?.Data;
        var currentPolicy = policyData?.Rules.FirstOrDefault(r => r.Name == policyName);

        if(policyData == null)
        {
            policyData = new StorageAccountManagementPolicyData()
            {
                Rules = new List<ManagementPolicyRule>()
            };
        }

        if (currentPolicy == null)
        {
            var definition = new ManagementPolicyDefinition(new ManagementPolicyAction
            {
                BaseBlob = new ManagementPolicyBaseBlob
                {
                    Delete = new DateAfterModification
                    {
                        DaysAfterCreationGreaterThan = logCollection.LogDurationDays,
                        DaysAfterModificationGreaterThan = null
                    }
                }
            })
            {
                Filters = new ManagementPolicyFilter(new[] { "blockBlob" })
                {
                    PrefixMatch = { $"logs/v1/{logCollection.TableName}/" }
                }
            };

            currentPolicy = new ManagementPolicyRule(policyName, ManagementPolicyRuleType.Lifecycle, definition)
            {
                IsEnabled = true
            };

            policyData.Rules.Add(currentPolicy);
        }
        else
        {
            if (currentPolicy.Definition.Actions.BaseBlob?.Delete != null)
            {
                currentPolicy.Definition.Actions.BaseBlob.Delete.DaysAfterCreationGreaterThan = logCollection.LogDurationDays;
                currentPolicy.Definition.Actions.BaseBlob.Delete.DaysAfterModificationGreaterThan = null;
            }
        }

        await storageAccount.GetStorageAccountManagementPolicy().CreateOrUpdateAsync(WaitUntil.Completed, policyData);
    }

    public async Task DeleteLifecyclePolicyAsync(string collectionName)
    {
        var armClient = GetArmClient();
        var storageAccount = await GetStorageAccountAsync(armClient);

        var managementPolicy = await GetManagementPolicyAsync(storageAccount);
        var policyData = managementPolicy.Data;

        var ruleName = $"logcollection-{collectionName}-lifecycle";

        // Find and remove the rule
        var existingRule = policyData.Rules.FirstOrDefault(r => r.Name == ruleName);
        if (existingRule == null)
        {
            throw new InvalidOperationException($"Lifecycle policy for log collection {collectionName} does not exist.");
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

    public ArmClient GetArmClient()
    {
        var credential = new ClientSecretCredential(
            AzureConfig.TenantId,
            AzureConfig.ClientId,
            AzureConfig.ClientSecret
        );
        return new ArmClient(credential);
    }

    public async Task<StorageAccountResource> GetStorageAccountAsync(ArmClient armClient)
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

    public async Task<StorageAccountManagementPolicyResource?> GetManagementPolicyAsync(StorageAccountResource storageAccount)
    {
        try
        {
            return await storageAccount.GetStorageAccountManagementPolicy().GetAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<DownloadedFile> DownloadFileAsync(string collectionName, string fileName)
    {
        // Initialize BlobServiceClient using AzureConfig connection string
        var blobServiceClient = new BlobServiceClient(AzureConfig.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(AzureConfig.ContainerName);

        var blobPath = GenerateBlobPath(collectionName, fileName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        try
        {
            // Download blob content stream
            var downloadResponse = await blobClient.DownloadStreamingAsync();

            // Read compressed bytes into memory
            byte[] compressedBytes;
            using (var downloadStream = downloadResponse.Value.Content)
            using (var memoryStream = new MemoryStream())
            {
                await downloadStream.CopyToAsync(memoryStream);
                compressedBytes = memoryStream.ToArray();
            }

            // Decompress using the appropriate compression strategy based on file name
            var compressionStrategy = _compressionFactory.GetStrategyFromFileName(fileName);
            var decompressedContent = compressionStrategy.Decompress(compressedBytes);

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

    private string GenerateBlobPath(string collectionName, string fileName)
    {
        return $"logs/v1/{collectionName}/{fileName}";
    }

    public class DownloadedFile
    {
        public bool Found;
        public string? Content;
    }
}
